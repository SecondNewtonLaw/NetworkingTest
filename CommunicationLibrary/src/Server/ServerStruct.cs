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
    public int connectedPeers;
    /// <summary>
    /// The way peers are identified.
    /// Key == Peer: IP
    /// Value == Peer: Public Name.
    /// </summary>
    public IDictionary<IPAddress, string> PeerIdentification { get; private set; }

    public readonly string SerializePeers()
    {
        return JsonConvert.SerializeObject(PeerIdentification);
    }
    /// <summary>
    /// Deserialize a string into a Dictinary with peer data.
    /// </summary>
    /// <param name="peers"></param>
    /// <exception cref="NullParameterException"></exception>
    public void DeserializePeers(string peers)
    {
        try
        {
            PeerIdentification = JsonConvert.DeserializeObject<IDictionary<IPAddress, string>>(peers)!;
        }
        catch when (peers is null)
        {
            throw new NullParameterException("Could not parse Peers, as the provided string was null.");
        }
        catch
        {
            throw;
        }
    }
}
