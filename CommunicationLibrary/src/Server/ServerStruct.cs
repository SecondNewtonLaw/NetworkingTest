namespace CommunicationLibrary;

/// <summary>
/// Struct containing information of the Server.
/// </summary>
public struct ServerStruct
{
    public ServerStruct()
    {
        connectedPeers = 0;
        // Avoid multiple enumeration.
        peerGuids = new List<string>();
    }
    public int connectedPeers;
    public IEnumerable<string> peerGuids;
}
