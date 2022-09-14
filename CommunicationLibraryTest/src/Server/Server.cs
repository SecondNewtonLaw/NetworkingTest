using System.Net;
using CommunicationLibrary;
using Spectre.Console;

namespace CommunicationLibraryTest;

public partial class ServerImplementation
{
    static CommunicationLibrary.Server srvInstance;
    public static async Task Start(ushort netPort)
    {
        srvInstance = new(netPort, IPAddress.Loopback);

        AnsiConsole.MarkupLine("Starting TCP Listener.");
        srvInstance.StartListener(delayBetweenChecks: 500, enableLogging: true);

        AnsiConsole.MarkupLine("Awaiting Connections...");
    }

    public static void Stop()
    {
        AnsiConsole.MarkupLine("Stopping TCP Listener...");
        srvInstance.StopListener();
        AnsiConsole.MarkupLine("Done.");
    }
}