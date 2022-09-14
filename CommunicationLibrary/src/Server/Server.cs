using System.Globalization;
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
    /// <summary>
    /// Is Logging Enabled
    /// </summary>
    private bool logging = false;
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
    /// <param name="delayBetweenChecks">The delay applied between each check of connections.</param>
    /// <param name="enableLogging">Defaults to false, should logging be enabled to the STDOUT</param>
    /// <returns>The Thread that is running the Server logic.</returns>
    public Thread StartListener(int delayBetweenChecks, bool enableLogging = false)
    {
        if (serverRunning)
            throw new AlreadyRunningException("The TCP Server is already running!");

        delay = delayBetweenChecks;
        logging = enableLogging;
        tcpListener.Start();
        Thread srvLoopThread = new(async () => await ServerLoop().ConfigureAwait(false))
        {
            Name = "P2P Server",
            IsBackground = true, // True, else program won't close.
        };
        srvLoopThread.Start();
        serverRunning = true;
        return srvLoopThread;
    }

    /// <summary>
    /// Stop listening for TCP Connections.
    /// </summary>
    /// <param name="waitForConnections">Should the method wait a period of time before terminating the TcpListener? Defaults to True</param>
    public void StopListener(bool waitForConnections = true)
    {
        stopServer = true;

        if (waitForConnections)
            Thread.Sleep(5000); // 5 Second delay.

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

            // Create a Thread that will handle the recieved connection.
            Thread responser = new(async () => await HandleConnectionAsync().ConfigureAwait(false));
            responser.Start();
        }
        serverRunning = false;
    }

    private async Task HandleConnectionAsync()
    {
        PeerIdentification identification = new();
        Socket clientSock = await tcpListener.AcceptSocketAsync().ConfigureAwait(false);
        string clientName = clientSock.RemoteEndPoint.ToString();

        LogToStdOut($"Connection established to {clientSock.RemoteEndPoint}.");
        // Set buffer sizes.
        clientSock.SendBufferSize = Constants.BUF_SIZE;
        clientSock.ReceiveBufferSize = Constants.BUF_SIZE;

        LogToStdOut("Creating WrappedSocket to wrap around default System.Net.Sockets.Socket.");

        WrappedSocket wrappedSock = new(clientSock, useEncodedMessage: true);

        while (true)
        {
            SocketMode modeOfSocket = await wrappedSock.GetSocketMode().ConfigureAwait(false);
            LogToStdOut($"Obtained SocketMode {modeOfSocket}.");

            if (modeOfSocket is SocketMode.Disconnect)
            {
                LogToStdOut($"Client {clientName} requested disconnect. Terminating Connection Controller Thread and Connection");
                wrappedSock.Dispose(); // Dispose of the WrappedSocket class.
                return;
            }

            long expectedSize = await wrappedSock.GetSocketContentLength().ConfigureAwait(false);
            LogToStdOut($"Determined content size to be of {expectedSize.ToString("G", CultureInfo.InvariantCulture)} bytes.");
            Object? content = await wrappedSock.GetSocketContent(modeOfSocket, expectedSize).ConfigureAwait(false);

            if (content is null && modeOfSocket is SocketMode.Hello)
            {
                // TODO: OnConnectionStablished event.
                // Hello Recieved, Call OnConnectionStablished.
                LogToStdOut($"Recieved Hello from {clientName}.");
                LogToStdOut($"Requesting Identify from {clientName}.");
                await wrappedSock.SendSocketMode(SocketMode.RequestIdentify).ConfigureAwait(false);
                continue;
            }

            if (modeOfSocket is SocketMode.EncodedMessage)
            {
                // The message is a string 120% percent sure.
                LogToStdOut($"Recieved EncodedMessage from {clientName}.");
                LogToStdOut($"Message is => {content as string}");
            }

            if (modeOfSocket is SocketMode.SendIdentify)
            {
                identification = (PeerIdentification)content!;
                Console.WriteLine($"Identified {clientName} as {identification.PeerName}.");
                clientName = identification.PeerName;
            }
        }

        // TODO: Implement events that will fire after ConnectionInformation for the current, running connection is created, then, add them to a dictionary to keep track of them.

        // ConnectionInformation cnnInfo = new(modeOfSocket, Encoding.UTF8.GetBytes((string)content!), wrappedSock);
    }
    private void LogToStdOut(string str)
    {
        if (logging)
            Console.WriteLine(str);
    }
}