using System.Text;

namespace CommunicationLibrary;

public struct ConnectionInformation
{
    public ConnectionInformation(SocketMode sockMode, byte[] receivedData, WrappedSocket socket)
    {
        socketMode = sockMode;
        _socketData = receivedData;
        this.socket = socket;
    }
    /// <summary>
    /// The mode in which the socket has been placed in.
    /// </summary>
    public readonly SocketMode socketMode;

    /// <summary>
    /// The data received from the socket
    /// </summary>
    private readonly byte[] _socketData;

    /// <summary>
    /// The WrappedSocket instance from which the Connection originates.
    /// </summary>
    public readonly WrappedSocket socket;

    /// <summary>
    /// Gets the data that the socket received.
    /// </summary>
    /// <returns>Gets the received data as a byte[].</returns>
    public readonly byte[] GetSocketData()
        => _socketData;

    /// <summary>
    /// Gets the data that the socket received.
    /// </summary>
    /// <param name="decodeAsBase64">Defaults to true, if false, it will just return the message raw, might mean a small performance boost.</param>
    /// <returns>Gets the received data as a string.</returns>
    public readonly string GetSocketData(bool decodeAsBase64 = true)
    {
        Span<byte> data = stackalloc byte[_socketData.Length];

        try
        {
            // Allocate memory in the stack, gotta go fast.

            if (!decodeAsBase64)
                return Encoding.UTF8.GetString(_socketData);

            string tmp = Encoding.UTF8.GetString(_socketData);

            // Attempt conversion.
            if (Convert.TryFromBase64String(tmp, data, out int writtenBytes))
            {
                // Get resultant bytes and return.
                return Encoding.UTF8.GetString(data);
            }

            return tmp;
        }
        finally
        {
            data.Clear();
        }
    }
}