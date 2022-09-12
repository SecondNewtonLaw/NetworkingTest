using System;
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