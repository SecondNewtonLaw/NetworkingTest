using System.Linq;
using System.Net;
using System.Net.Sockets;

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
        const int bufferSize = 131072; // 8192*16
        const int timeOutRecieve = 5000;
        while (!stopServer)
        {
            await Task.Delay(delay).ConfigureAwait(false);

            // There aren't any pending connections, don't waste resources.
            if (!PendingConnections())
                continue;

            Socket clientSock = await tcpListener.AcceptSocketAsync().ConfigureAwait(false);

            // Set buffer sizes.
            clientSock.SendBufferSize = bufferSize;
            clientSock.ReceiveBufferSize = bufferSize;

            WrappedSocket wrappedSock = new(clientSock, useEncodedMessage: true);

            SocketMode modeOfSocket = await wrappedSock.GetSocketMode().ConfigureAwait(false);

            byte remainingTries = 3;
            List<byte> buffer = new();

            while (remainingTries > 0)
            {
                byte[] temporalBuffer = new byte[1024];
                int read = await clientSock.ReceiveAsync(temporalBuffer, SocketFlags.None).ConfigureAwait(false);

                buffer.AddRange(temporalBuffer);

                if (read == 0)
                {
                    // Wait a timeout, expecting new data in.
                    await Task.Delay(timeOutRecieve).ConfigureAwait(false);
                    remainingTries--; // Decrease remaining tries.
                }
            }

            // TODO: Implement events that will fire after ConnectionInformation for the current, running connection is created, then, add them to a dictionary to keep track of them.

            ConnectionInformation cnnInfo = new(modeOfSocket, buffer.ToArray(), wrappedSock);
        }
        serverRunning = false;
    }
}