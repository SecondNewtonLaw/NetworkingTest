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
    public static void StopClient()
    {
        if (tcpClient is null || netStream is null)
            throw new InvalidOperationException("You can not close a connection that does not exist at all!");

        tcpClient.Client.Disconnect(reuseSocket: false);
        tcpClient.Client.Dispose();
        tcpClient.Dispose();
        netStream.Dispose();
    }
    public static async Task SendHello()
    {
        if (!tcpClient.Connected)
            throw new InvalidOperationException("Can not send HELLO SocketMode if there is no connection in place!");

        tcpClient.SendBufferSize = Constants.BUF_SIZE;
        tcpClient.ReceiveBufferSize = Constants.BUF_SIZE;

        netStream.WriteByte((byte)SocketMode.Hello);
        await netStream.WriteAsync(BitConverter.GetBytes(ulong.MinValue)).ConfigureAwait(false); // Size is 0.

        await netStream.FlushAsync().ConfigureAwait(false); // Flush.
    }

    internal static void SendDisconnect()
    {
        if (!tcpClient.Connected)
            throw new InvalidOperationException("Can not disconnect if there is no connection in place!");

        netStream.WriteByte((byte)SocketMode.Disconnect); // Send SocketMode.Disconnect
        netStream.Flush(); // Flush.
    }

    public static async Task SendMessage()
    {
        if (!tcpClient.Connected)
            throw new InvalidOperationException("Can not send EncodedMessage SocketMode if there is no connection in place!");

        tcpClient.ReceiveBufferSize = Constants.BUF_SIZE;

        while (true)
        {
            string? msg = null;
            Console.Write($"Send to {tcpClient.Client.RemoteEndPoint} > ");

            while (msg is null)
                msg = Console.ReadLine();

            Console.WriteLine($"Sending message to {tcpClient.Client.RemoteEndPoint}...");

            netStream.WriteByte((byte)SocketMode.EncodedMessage); // Send socket mode.
            await netStream.FlushAsync().ConfigureAwait(false); // Flush.

            byte[] data = Encoding.UTF8.GetBytes(Convert.ToBase64String(Encoding.UTF8.GetBytes(msg)));
            byte[] dataSizeAsLong = BitConverter.GetBytes(data.LongLength);

            tcpClient.SendBufferSize = data.Length;

            Console.WriteLine("Content: " + Encoding.UTF8.GetString(data));
            Console.WriteLine($"Sending {BitConverter.ToInt64(dataSizeAsLong)} bytes as Length, with true size {data.LongLength} bytes");
            await netStream.WriteAsync(dataSizeAsLong).ConfigureAwait(false);

            await netStream.WriteAsync(data).ConfigureAwait(false);
            await netStream.FlushAsync().ConfigureAwait(false); // Flush.
            Console.WriteLine($"Message Sent to {tcpClient.Client.RemoteEndPoint}.");
        }
    }
}