
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
    /// Used to send a raw, UTF-8 string to the other party.
    /// </summary>
    Message = 1,
    /// <summary>
    /// Used to send a Base64 encoded, UTF-8, string to the other party
    /// </summary>
    EncodedMessage = 2,
    /// <summary>
    /// Used to send a file in a Base64 encoded string to the other party.
    /// </summary>
    File = 3,
}