using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace CommunicationLibrary;

public class WrappedSocket : IEquatable<WrappedSocket>
{
    #region Properties

    /// <summary>
    /// The <see cref="WrappedSocket"/> underlying socket, used to do ALL the requests.
    /// </summary>
    private readonly Socket _underlyingSocket;

    /// <summary>
    /// Wether or not the message should be encoded into Base64
    /// </summary>
    private readonly bool encodeMessage;

    /// <summary>
    /// The Object's HashCode, only generated when calling <see cref="GetHashCode()"/>
    /// </summary>
    private int? hashCode = null;

    #endregion Properties

    #region Constructors

    public WrappedSocket(Socket socket, bool useEncodedMessage)
    {
        _underlyingSocket = socket;
        encodeMessage = useEncodedMessage;
    }

    #endregion Constructors

    #region Overrides

    /// <summary>
    /// Asserts wether the underlying <see cref="System.Net.Sockets.Socket"/> is the same as the one in <paramref name="socket"/>.
    /// </summary>
    /// <param name="socket">The <see cref="WrappedSocket"/> to compare towards.</param>
    /// <returns>True if the <see cref="System.Net.Sockets.Socket"/> instances match, else False.</returns>
    public bool Equals(WrappedSocket? socket)
    {
        if (socket is not null)
            return this._underlyingSocket == socket._underlyingSocket;

        // The WrappedSocket instance to compare towards is null, it won't be equal regardless.
        return false;
    }

    /// <summary>
    /// Asserts wether the underlying <see cref="System.Net.Sockets.Socket"/> is the same as the one in <paramref name="obj"/>.
    /// </summary>
    /// <param name="obj">The <see cref="Object"/> to compare towards.</param>
    /// <returns>True if the <see cref="System.Net.Sockets.Socket"/> instances match, else False.</returns>
    /// <remarks>if the object is not of Type <see cref="WrappedSocket"/> the assertion will ne False..</remarks>
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (obj.GetType() != typeof(WrappedSocket))
            return false;

        return Equals(obj as WrappedSocket);
    }

    /// <summary>
    /// Receive the object's hashcode.
    /// </summary>
    /// <returns>A Randomly generated hash that is consistent through calls of the method.</returns>
    public override int GetHashCode()
    {
        // The HashCode must remain consistent.
        if (hashCode.HasValue)
            return hashCode.Value;

        static int BitShift(int value, int positions)
        {
            // Save the existing bit pattern, but interpret it as an unsigned integer.
            uint number = BitConverter.ToUInt32(BitConverter.GetBytes(value), 0);
            // Preserve the bits to be discarded.
            uint wrapped = number >> (32 - positions);
            // Shift and wrap the discarded bits.
            return BitConverter.ToInt32(BitConverter.GetBytes((number << positions) | wrapped), 0);
        }
        int rndVal = RandomNumberGenerator.GetInt32(RandomNumberGenerator.GetInt32(913672193));
        int shift = RandomNumberGenerator.GetInt32(RandomNumberGenerator.GetInt32(24));

        hashCode = BitShift(rndVal, shift);
        return (int)hashCode;
    }

    #endregion Overrides

    #region Socket Implementation (Custom Methods)

    /// <summary>
    /// Get the Underluing Socket used to communicate.
    /// </summary>
    /// <returns>The Socket used to communicate internally.</returns>
    /// <remarks>Using this might lead to unexpected behavior if done incorrectly, think carefully.</remarks>
    public Socket GetUnderlyingSocket()
        => _underlyingSocket;

    /// <summary>
    /// Get the corresponding <see cref="SocketMode"/> of a request.
    /// </summary>
    /// <returns>A <see cref="Task"/> with Type <see cref="SocketMode"/> representing the on-going asynchronous operation.</returns>
    /// <exception cref="InvalidDataException">Thrown if the packet is not the expected one.</exception>
    /// <remarks>
    /// This method should be call after accepting the connection,
    /// this is due to the deterministic nature of it,
    /// being that the <see cref="SocketMode"/> will be representing by a one <see cref="Byte"/> sized packet at the start of the connection.
    /// </remarks>
    public async Task<SocketMode> GetSocketMode()
    {
        byte[] socketMode = new byte[1];
        int receivedLength = await _underlyingSocket.ReceiveAsync(socketMode, SocketFlags.None).ConfigureAwait(false);

        // Wait for the SocketMode, when received, if it is NOT 1 (Expected size, throw an exception)
        if (receivedLength is not 1)
            throw new InvalidDataException($"The other party has transmitted invalid data, namely, {receivedLength.ToString(CultureInfo.CurrentCulture)} bytes instead of 1.");

        return (SocketMode)socketMode[0];
    }
    /// <summary>
    /// Get the content of the Underlying Socket, but, treat it as <paramref name="modeOfSocket"/> specifies.
    /// </summary>
    /// <param name="modeOfSocket">The mode in which the data should be treated.</param>
    /// <returns>A Task of Type Object containing the serialized data.</returns>
    /// <exception cref="NotImplementedException">Thrown if the API method is not implemented.</exception>
    /// <exception cref="IncompleteDataException">Thrown if Base64 encoded string is incorrect, only applies to cases in which <paramref name="modeOfSocket"/> is <see cref="CommunicationLibrary.SocketMode.EncodedMessage"/>.</exception>
    public async Task<Object?> GetSocketContent(SocketMode modeOfSocket)
    {
        // The Hello packet is meant to indicate a new connection, no other data is expected.
        if (modeOfSocket is SocketMode.Hello)
            return null;

        List<byte> receivedBytes = new();
        byte remainingTries = 3;

        while (remainingTries > 0)
        {
            byte[] temporalBuffer = new byte[1024];
            int read = await _underlyingSocket.ReceiveAsync(temporalBuffer, SocketFlags.None).ConfigureAwait(false);

            receivedBytes.AddRange(temporalBuffer);

            if (read is 0)
            {
                // Wait a timeout, expecting new data to flow in...
                await Task.Delay(Constants.RECIEVE_TIMEOUT).ConfigureAwait(false);
                remainingTries--; // Decrease remaining tries.
            }
        }

        byte[] arrayedBuffer = receivedBytes.ToArray();

        receivedBytes.Clear(); // Clear list.
        if (modeOfSocket is SocketMode.Message)
        {
            return Encoding.UTF8.GetString(arrayedBuffer);
        }

        if (modeOfSocket is SocketMode.EncodedMessage)
        {
            string tmp = Encoding.UTF8.GetString(arrayedBuffer);
            byte[] buffer = new byte[tmp.Length];
            // Attempt conversion.
            if (Convert.TryFromBase64String(tmp, buffer, out int writtenBytes))
            {
                // Get resultant bytes and return.
                return Encoding.UTF8.GetString(buffer);
            }
            throw new IncompleteDataException($"The SocketMode indicated an Encoded, Base64 message, the length of the final string is {writtenBytes.ToString("G", CultureInfo.CurrentCulture)} bytes, but the expected one was {tmp.Length.ToString("G", CultureInfo.CurrentCulture)}. Have you verified parameter?");
        }

        if (modeOfSocket is SocketMode.Discover)
        {
            // TODO: Implement a way to share Peers between Peers, or just to return the Server list.
        }

        if (modeOfSocket is SocketMode.File)
        {
            // TODO: Implement a way to share Files between users. or just copy the byte[] to a MemoryStream and return it as such.
            // ! Likely doing option (B) 
        }

        throw new NotImplementedException("Likely missing API implementation, or just, straightforward, forgot to finish a part of it.");
    }

    #endregion Socket Implementation (Custom Methods)

    #region Socket Implementation (Non-Custom methods)

    /// <summary>
    /// Send a message towards the other connected party.
    /// </summary>
    /// <param name="message">The message as a string.</param>
    /// <exception cref="IncompleteDataException">Thrown if the socket could not send data to the other party.</exception>
    /// <returns>A Task representing the on-going asynchronous operation.</returns>
    public async Task SendMessage(string message)
    {
        SocketMode socketSendMode = SocketMode.Message; // Default.
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        byte[] finalMessage = messageBytes; // Copy array.

        if (encodeMessage)
        {
            socketSendMode = SocketMode.EncodedMessage;
            // Make into Base64 encoding IF requested in the constructor
            string encoded = Convert.ToBase64String(messageBytes);
            finalMessage = Encoding.UTF8.GetBytes(encoded);
        }

        byte[] socketMode = new byte[] { (byte)socketSendMode };

        int socketModeSentLength = await _underlyingSocket.SendAsync(socketMode, SocketFlags.None).ConfigureAwait(false);

        if (socketModeSentLength != socketMode.Length)
            throw new IncompleteDataException("The socket failed to send the required data to set the SocketMode!");

        int sentData = await _underlyingSocket.SendAsync(finalMessage, SocketFlags.None).ConfigureAwait(false);

        if (sentData != finalMessage.Length)
            throw new IncompleteDataException("The socket failed to send all the data!");
    }

    #endregion Socket Implementation (Non-Custom methods)
}