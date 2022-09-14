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
    public string PeerName { get; init; }
    /// <summary>
    /// The port that will be used to communicate between peers.
    /// </summary>
    [JsonProperty("port")]
    public int OpenPort { get; init; }
    /// <summary>
    /// The <see cref="IPAddress"/> of the peer represented as a string.
    /// </summary>
    /// <remarks>The <see cref="IPAddress"/> should be Internet Protocol Version 4.</remarks>
    [JsonProperty("ip")]
    public string PeerIp { get; init; }

    /// <summary>
    /// Serializes the current instance into a JSON.
    /// </summary>
    /// <returns>a JSON representing the <see cref="PeerInformation"/>.</returns>
    public readonly string GetSerializedAsJson()
    {
        return JsonConvert.SerializeObject(this);
    }
    /// <summary>
    /// The IPAddress of the peer.
    /// </summary>
    [JsonIgnore]
    public readonly IPAddress IPV4
    {
        get
        {
            return IPAddress.Parse(PeerIp);
        }
    }
}