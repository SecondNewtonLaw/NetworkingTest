using System;
using System.Net;
using System.Net.Sockets;

namespace CommunicationLibrary;

/// <summary>
/// A Struct used to send data to identify a Peer.
/// </summary>
public struct PeerIdentification
{
    /// <summary>
    /// The public name of the peer.
    /// </summary>
    public string peerName;
    /// <summary>
    /// The port that will be used to communicate between peers.
    /// </summary>
    public int openPort;
}