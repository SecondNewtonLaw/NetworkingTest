
namespace CommunicationLibrary;

/// <summary>
/// Enumeration used to communicate between two <see cref="WrappedSocket"/>s on different systems.
/// </summary>
public enum SocketMode
{
    /// <summary>
    /// Used to communicate to the remote party that the connection has been stablished.
    /// </summary>
    /// <remarks>No data shall be sent, only the Hello packet</remarks>
    Hello = 0,
    /// <summary>
    /// Used to communicate to the remote party that you are terminating the connection.
    /// </summary>
    /// <remarks>No data shall be sent, only the Disconnect packet</remarks>
    Disconnect,
    /// <summary>
    /// Used to request the remote party for their <see cref="PeerIdentification"/> struct.
    /// </summary>
    /// <remarks>This operation should be done using Base64 Encoding.</remarks>
    RequestIdentify,
    /// <summary>
    /// Used to communicate to the remote party that you will send your <see cref="PeerIdentification"/> struct.
    /// </summary>
    /// <remarks>This operation should be done using Base64 Encoding.</remarks>
    SendIdentify,
    /// <summary>
    /// Used to query the remote party for other remote parties.
    /// </summary>
    /// <remarks>This operation should be done using Base64 Encoding.</remarks>
    Discover,
    /// <summary>
    /// Used to send a raw, UTF-8 string to the other party.
    /// </summary>
    Message,
    /// <summary>
    /// Used to send a Base64 encoded, UTF-8 string to the other party
    /// </summary>
    EncodedMessage,
    /// <summary>
    /// Used to send a file in a Base64 encoded string to the other party.
    /// </summary>
    File,
}