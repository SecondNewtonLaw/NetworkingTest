using System.Globalization;
using CommunicationLibrary;

namespace CommunicationLibraryTest;
internal static class Program
{
    private static async Task Main(string[] args)
    {
        bool runAsServer = args.Contains("--server", StringComparer.InvariantCultureIgnoreCase);
        bool runAsClient = args.Contains("--client", StringComparer.InvariantCultureIgnoreCase);

        // TODO: Implement Client and Server logic from CommunicationLibrary utilizing the provided, exposed 'API'

        if (runAsServer && runAsClient)
        {
            Console.WriteLine("Can not start both the server and the client at the same time! Exiting...");
            Environment.Exit(0);
        }
    }
}
