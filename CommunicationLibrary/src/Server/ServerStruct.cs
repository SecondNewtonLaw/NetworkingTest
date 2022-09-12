using System.Net;
using Newtonsoft.Json;

namespace CommunicationLibrary;

/// <summary>
/// Struct containing information on the Server.
/// </summary>
public struct ServerStruct
{
    public ServerStruct()
    {
        connectedPeers = 0;
        // Avoid multiple enumeration.
        PeerIdentification = new Dictionary<IPAddress, string>();
    }
    /// <summary>
    /// The amount of connected peers.
    /// </summary>
    public int connectedPeers;
    /// <summary>
    /// The way peers are identified.
    /// Key == Peer: IP
    /// Value == Peer: Public Name.
    /// </summary>
    public IDictionary<IPAddress, string> PeerIdentification { get; private set; }

    /// <summary>
    /// Serializes the current instance's <see cref="PeerIdentification"/> Dictionary into a string.
    /// </summary>
    /// <returns>A new <see cref="String"/> instance, its contetns being the serialized <see cref="PeerIdentification"/>.</returns>
    public readonly string SerializePeers()
        => JsonConvert.SerializeObject(PeerIdentification);

    /// <summary>
    /// Deserialize a <see cref="String"/> into a Dictinary with peer data.
    /// </summary>
    /// <param name="peers">The <see cref="String"/> containing the serialized data of the <see cref="PeerIdentification"/> Dictionary</param>
    /// <exception cref="NullParameterException">The <see cref="String"/> in the <paramref name="peers"/> parameter is null.</exception>
    public void DeserializePeers(string peers)
    {
        try
        {
            PeerIdentification = JsonConvert.DeserializeObject<IDictionary<IPAddress, string>>(peers)!;
        }
        catch when (peers is null)
        {
            throw new ArgumentNullException(peers);
        }
        catch
        {
            throw; // Throw the caught Exception.
        }
    }
}
