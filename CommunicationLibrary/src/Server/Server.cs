using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CommunicationLibrary;

/// <summary>
/// Clsas containing the Server logic.
/// </summary>
public class Server
{
    #region Properties

    /// <summary>
    /// The delay in milliseconds between the execution of a <see cref="ServerLoop()"/> operation.
    /// </summary>
    private int delay;

    /// <summary>
    /// Is the server set to stop?
    /// </summary>
    private bool stopServer = false;

    /// <summary>
    /// Is the server currently running?
    /// </summary>
    private bool serverRunning = false;

    /// <summary>
    /// The IPAddress the server is listening.
    /// </summary>
    private readonly IPAddress listenToAddress;

    /// <summary>
    /// The networking port the server is listening.
    /// </summary>
    private readonly ushort listenToPort;

    /// <summary>
    /// The Server's connection listener
    /// </summary>
    private readonly TcpListener tcpListener;

    #endregion Properties

    public Server(ushort networkPort, IPAddress listenTo)
    {
        listenToPort = networkPort;
        listenToAddress = listenTo;
        tcpListener = new(listenToAddress, listenToPort);
    }

    /// <summary>
    /// Start listening for TCP Connections.
    /// </summary>
    public Thread StartListener(int delayBetweenChecks)
    {
        if (serverRunning)
            throw new AlreadyRunningException("The TCP Server is already running!");

        tcpListener.Start();
        delay = delayBetweenChecks;
        Thread srvLoopThread = new(async () => await ServerLoop().ConfigureAwait(false))
        {
            Name = "P2P Server",
        };
        srvLoopThread.Start();
        serverRunning = true;
        return srvLoopThread;
    }
    /// <summary>
    /// Stop listening for TCP Connections.
    /// </summary>
    public void StopListener()
    {
        stopServer = true;
        tcpListener.Stop();
    }
    /// <summary>
    /// Check for available, pending TCP connections.
    /// </summary>
    /// <returns>True if there are connections ready to be processed.</returns>
    public bool PendingConnections()
    {
        if (!serverRunning)
            throw new NotRunningException("The server is not running, therefore, there are NO pending connections. Have you started the server before running this method?");

        return tcpListener.Pending();
    }
    private async Task ServerLoop()
    {
        while (!stopServer)
        {
            await Task.Delay(delay).ConfigureAwait(false);

            // There aren't any pending connections, don't waste resources.
            if (!PendingConnections())
                continue;

            Socket clientSock = await tcpListener.AcceptSocketAsync().ConfigureAwait(false);

            // Set buffer sizes.
            clientSock.SendBufferSize = Constants.BUF_SIZE;
            clientSock.ReceiveBufferSize = Constants.BUF_SIZE;

            WrappedSocket wrappedSock = new(clientSock, useEncodedMessage: true);

            SocketMode modeOfSocket = await wrappedSock.GetSocketMode().ConfigureAwait(false);

            Object? content = await wrappedSock.GetSocketContent(modeOfSocket).ConfigureAwait(false);

            if (content is null && modeOfSocket is SocketMode.Hello)
            {
                // TODO: OnConnectionStablished event.
                // Hello Recieved, Fire OnConnectionStablished Event.
            }

            // TODO: Implement events that will fire after ConnectionInformation for the current, running connection is created, then, add them to a dictionary to keep track of them.

            ConnectionInformation cnnInfo = new(modeOfSocket, Encoding.UTF8.GetBytes((string)content!), wrappedSock);
        }
        serverRunning = false;
    }
}