using System.Net;
using System.Net.Sockets;
using System.Text;
using CommunicationLibrary;
using Spectre.Console;

namespace CommunicationLibraryTest;

public partial class ClientImplementation
{
    private static TcpClient tcpClient;
    private static NetworkStream netStream;
    public static async Task StartClient(IPEndPoint target)
    {
        await (tcpClient ??= new()).ConnectAsync(target);
        netStream = tcpClient.GetStream();
    }
    public static async Task SendHello()
    {
        if (!tcpClient.Connected)
            throw new InvalidOperationException("Can not send HELLO SocketMode if there is no connection in place!");

        tcpClient.SendBufferSize = Constants.BUF_SIZE;
        tcpClient.ReceiveBufferSize = Constants.BUF_SIZE;

        netStream.WriteByte((byte)SocketMode.Hello);
        await netStream.FlushAsync().ConfigureAwait(false); // Flush.
    }
    public static async Task SendMessage()
    {
        if (!tcpClient.Connected)
            throw new InvalidOperationException("Can not send HELLO SocketMode if there is no connection in place!");

        tcpClient.SendBufferSize = Constants.BUF_SIZE;
        tcpClient.ReceiveBufferSize = Constants.BUF_SIZE;

        while (true)
        {
            string? msg = null;
            while (msg is null)
                msg = Console.ReadLine();

            Console.WriteLine("Sending message...");

            netStream.WriteByte((byte)SocketMode.EncodedMessage);
            await netStream.FlushAsync().ConfigureAwait(false); // Flush.

            await netStream.WriteAsync(Encoding.ASCII.GetBytes(Convert.ToBase64String(Encoding.UTF8.GetBytes(msg))));
            await netStream.FlushAsync().ConfigureAwait(false); // Flush.
            Console.WriteLine("Message Sent.");
        }
    }
}