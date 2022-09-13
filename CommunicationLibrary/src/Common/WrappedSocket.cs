using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace CommunicationLibrary;

/// <summary>
/// Wraps around the normal <see cref="System.Net.Sockets.Socket"/> class to provide easy-to-use functionality.
/// </summary>
/// <remarks>Implements <see cref="IDisposable"/> and IEquatable Interfaces.</remarks>
public class WrappedSocket : IEquatable<WrappedSocket>, IDisposable
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
    /// <summary>
    /// Has this Class been disposed of?
    /// </summary>
    private bool disposedValue;

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
        ThrowOnDisposed();

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
        ThrowOnDisposed();

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

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // If the socket is connected, end the connection.
                if (_underlyingSocket.Connected)
                    _underlyingSocket.Disconnect(reuseSocket: false);
            }
            // Dispose of socket.
            _underlyingSocket.Dispose();

            // Nullify parameters.
            hashCode = null;

            // Mark process as completed.
            disposedValue = true;
        }
    }
    // Finalizer, aka, OnDestroy
    ~WrappedSocket()
    {
        Dispose(disposing: true);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion Overrides

    #region Socket Implementation (Custom Methods)
    /// <summary>
    /// Gets if the Underlying Socket has been disposed along with any other Managed objects.
    /// </summary>
    /// <returns>True if they were disposed of, else False.</returns>
    public bool IsSocketDisposed()
        => disposedValue;
    /// <summary>
    /// Rises an <see cref="ObjectDisposedException"/> if the object has been disposed
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if the object is, indeed, disposed.</exception>
    private void ThrowOnDisposed()
    {
        if (IsSocketDisposed())
            throw new ObjectDisposedException("This object has been disposed, and you can no longer access it!");
    }

    /// <summary>
    /// Get the Underluing Socket used to communicate.
    /// </summary>
    /// <returns>The Socket used to communicate internally.</returns>
    /// <remarks>Using this might lead to unexpected behavior if done incorrectly, think carefully.</remarks>
    public Socket GetUnderlyingSocket()
    {
        ThrowOnDisposed();
        return _underlyingSocket;
    }

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
        ThrowOnDisposed();
        byte[] socketMode = new byte[1];
        int receivedLength = await _underlyingSocket.ReceiveAsync(socketMode, SocketFlags.None).ConfigureAwait(false);

        // Wait for the SocketMode, when received, if it is NOT 1 (Expected size, throw an exception)
        if (receivedLength is not 1)
            throw new InvalidDataException($"The other party has transmitted invalid data, namely, {receivedLength.ToString(CultureInfo.CurrentCulture)} bytes instead of 1.");

        return (SocketMode)socketMode[0];
    }

    /// <summary>
    /// Reads the next 64 bits of data, which indicate the length of the content in bytes.
    /// </summary>
    /// <returns>The length of the content that the socket will recieve next.</returns>
    /// <exception cref="IncompleteDataException">Thrown if the length is not corresponding the size of a long, which it should.</exception>
    public async Task<long> GetSocketContentLength()
    {
        ThrowOnDisposed();
        byte[] length = new byte[8];

        int read = await _underlyingSocket.ReceiveAsync(length, SocketFlags.None);

        if (read != sizeof(long))
            throw new IncompleteDataException("The data sent was incomplete or trimmed.");

        return BitConverter.ToInt64(length);
    }

    /// <summary>
    /// Get the content of the Underlying Socket, but, treat it as <paramref name="modeOfSocket"/> specifies.
    /// </summary>
    /// <param name="modeOfSocket">The mode in which the data should be treated.</param>
    /// <returns>A Task of Type Object containing the serialized data.</returns>
    /// <exception cref="NotImplementedException">Thrown if the API method is not implemented.</exception>
    /// <exception cref="FormatException">Thrown if Base64 encoded string is incorrect, only applies to cases in which <paramref name="modeOfSocket"/> is <see cref="CommunicationLibrary.SocketMode.EncodedMessage"/>.</exception>
    /// <exception cref="IncompleteDataException">Thrown if the read size is not equal to the expectedSize</exception>
    public async Task<Object?> GetSocketContent(SocketMode modeOfSocket, long expectedSize)
    {
        ThrowOnDisposed();
        // The Hello packet is meant to indicate a new connection, no other data is expected.
        if (modeOfSocket is SocketMode.Hello)
            return null;

        List<byte> receivedBytes = new();
        byte remainingTries = 3;
        long read = 0, readCurrent = 0;

        // Get last recieve buffer
        int lastRBuff = _underlyingSocket.ReceiveBufferSize;
        int newRBuff = (int)expectedSize;
        while (remainingTries > 0)
        {
            _underlyingSocket.ReceiveBufferSize = newRBuff;
            // Read all data that was meant to read.
            if (read == expectedSize)
                break;

            byte[] temporalBuffer = new byte[expectedSize]; // buffer
            if (_underlyingSocket.Available == expectedSize)
                readCurrent = await _underlyingSocket.ReceiveAsync(temporalBuffer, SocketFlags.None).ConfigureAwait(false);

            receivedBytes.AddRange(temporalBuffer);

            read += readCurrent;

            if (readCurrent is 0)
            {
                // Wait a timeout, waiting for new data to flow in...
                await Task.Delay(Constants.RECIEVE_TIMEOUT).ConfigureAwait(false);
                remainingTries--; // Decrease remaining tries before giving up.
            }
            readCurrent = 0;
            await Task.Delay(100).ConfigureAwait(false);
        }
        _underlyingSocket.ReceiveBufferSize = lastRBuff;
        if (read != expectedSize)
            throw new IncompleteDataException($"The socket promised {expectedSize} bytes of data, but recieved {read} bytes instead.");

        byte[] arrayedBuffer = receivedBytes.ToArray();

        receivedBytes.Clear(); // Clear list.
        if (modeOfSocket is SocketMode.Message)
        {
            return Encoding.UTF8.GetString(arrayedBuffer);
        }

        if (modeOfSocket is SocketMode.EncodedMessage)
        {
            // ! Double conversion explanation:
            // 
            // When sending UTF-8 encoded messages via sockets the Base64 decoder will fail to convert it back, to work around this 
            // we encode the Base64 encoded, UTF-8 message into ASCII Base64 msg, and do the process in reverse, ASCII Base64 -> UTF-8 BASE64 -> UTF-8 String.
            // 
            // ! New bug!
            // I, trimmed the data based on a length provided, now, for some reason, it seems like the Base64 string convertor doesn't like that much.
            // So it fails. I'm still working on a fix, but for now it will be sent back to caller anyways.

            // ! NOTE: Convert.TryFromBase64String(); == Broken.
            // ! Previous work arounds are not necessary when using Convert.FromBase64String(); instead of Convert.TryFromBase64String();

            string tmp = Encoding.UTF8.GetString(arrayedBuffer);

            try
            {
                byte[] bytes = Convert.FromBase64String(tmp);

                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                throw;
            }
        }

        if (modeOfSocket is SocketMode.Discover)
        {
            // TODO: Implement a way to share Peers between Peers, or just to return the Server list.

        }

        if (modeOfSocket is SocketMode.File)
        {
            // Return byte[] as a MemoryStream.
            return new MemoryStream(arrayedBuffer);
        }

        throw new NotImplementedException("Likely missing API implementation, or just, straightforward, forgot to finish a part of it.");
    }
    public async Task<PeerIdentification> GetPeerIdentificationAsync()
    {
        ThrowOnDisposed();
        throw new NotImplementedException("Method not implemented!");
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
        ThrowOnDisposed();
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