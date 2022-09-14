using System;
using System.Net;
using System.Net.Sockets;
using System.Security;
using Newtonsoft.Json;

namespace CommunicationLibrary;

/// <summary>
/// A Struct used to send data to identify a Peer.
/// </summary>
[Serializable]
public struct PeerIdentification
{
    /// <summary>
    /// The public name of the peer.
    /// </summary>
    [JsonProperty("name")]
    public string peerName;
    /// <summary>
    /// The port that will be used to communicate between peers.
    /// </summary>
    [JsonProperty("port")]
    public int openPort;
    /// <summary>
    /// The <see cref="IPAddress"/> of the peer.
    /// </summary>
    /// <remarks>The <see cref="IPAddress"/> should be Internet Protocol Version 4.</remarks>
    [JsonProperty("ip")]
    internal IPAddress peerIp;

    /// <summary>
    /// Serializes the current instance into a JSON.
    /// </summary>
    /// <returns>a JSON representing the <see cref="PeerInformation"/>.</returns>
    public readonly string GetSerializedAsJson()
    {
        return JsonConvert.SerializeObject(this);
    }
}