using System.Globalization;
using System.Net;
using CommunicationLibrary;

namespace CommunicationLibraryTest;
internal static class Program
{
    private static async Task Main(string[] args)
    {
        bool runAsServer = args.Contains("--server", StringComparer.InvariantCultureIgnoreCase);
        bool runAsClient = args.Contains("--client", StringComparer.InvariantCultureIgnoreCase);

        // TODO: Implement Client and Server logic from CommunicationLibrary utilizing the provided, exposed 'API'.

        Console.CancelKeyPress += (x, a) =>
        {
            if (x is not null)
                Console.WriteLine("Recived SIGINT from " + x.GetType().FullName);
            else
                Console.WriteLine("Recived SIGINT.");

            Console.WriteLine("Exiting Gracefully...");

            if (runAsServer)
            {
                ServerImplementation.Stop();
            }

            if (runAsClient)
            {
                ClientImplementation.SendDisconnect();
                ClientImplementation.StopClient();
            }
        };

        if ((runAsServer && runAsClient) || (!runAsServer && !runAsClient))
        {
            Console.WriteLine("Can not start client or server! Exiting...");
            Environment.Exit(0);
        }

        if (runAsServer)
        {
            await ServerImplementation.Start(1337).ConfigureAwait(false);
        }
        if (runAsClient)
        {
            await ClientImplementation.StartClient(new(IPAddress.Loopback, 1337)).ConfigureAwait(false);
            Thread listener = new(async () => await ClientImplementation.ListenerLoop().ConfigureAwait(false));
            listener.Start();
            await ClientImplementation.SendHello().ConfigureAwait(false);
            await ClientImplementation.SendMessage().ConfigureAwait(false);
        }

        await Task.Delay(-1).ConfigureAwait(false);
    }
}
