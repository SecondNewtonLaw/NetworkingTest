
namespace CommunicationLibrary;

/// <summary>
/// Enumeration used to communicate between two <see cref="WrappedSocket"/>s on different systems.
/// </summary>
public enum SocketMode
{
    /// <summary>
    /// Used to communicate to the remote party that the connection has been stablished.
    /// </summary>
    Hello = 0,
    /// <summary>
    /// Used to communicate to the remote party that you are terminating the connection.
    /// </summary>
    Disconnect = 1,
    /// <summary>
    /// Used to request the remote party for their <see cref="PeerIdentification"> struct.
    /// </summary>
    RequestIdentify = 2,
    /// <summary>
    /// Used to communicate to the remote party that you will send your <see cref="PeerIdentification"> struct.
    /// </summary>
    SendIdentify = 3,
    /// <summary>
    /// Used to query the remote party for other remote parties.
    /// </summary>
    Discover = 4,
    /// <summary>
    /// Used to send a raw, UTF-8 string to the other party.
    /// </summary>
    Message = 5,
    /// <summary>
    /// Used to send a Base64 encoded, UTF-8, string to the other party
    /// </summary>
    EncodedMessage = 6,
    /// <summary>
    /// Used to send a file in a Base64 encoded string to the other party.
    /// </summary>
    File = 7,
}