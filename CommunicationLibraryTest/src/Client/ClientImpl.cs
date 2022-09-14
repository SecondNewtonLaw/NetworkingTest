using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using CommunicationLibrary;
using Newtonsoft.Json;
using Spectre.Console;

namespace CommunicationLibraryTest;

public partial class ClientImplementation
{
    private struct ipv4API
    {
        [JsonProperty("ip")]
        public string ip;
    }
    private static PeerIdentification identification;
    private static TcpClient tcpClient;
    private static NetworkStream netStream;
    private static bool identificationSent = false;
    public static async Task StartClient(IPEndPoint target)
    {
        await (tcpClient ??= new()).ConnectAsync(target).ConfigureAwait(false);
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

    internal static async Task ListenerLoop()
    {
        if (!tcpClient.Connected)
            throw new InvalidOperationException("Can not listen for data if there is no connection in place!");

        // List<byte> byteBuffer = new(); // Keep a buffer.
        WrappedSocket wrapped = new(tcpClient.Client, useEncodedMessage: true); // Start a WrappedSocket to communicate with ease.
        while (true)
        {
            if (tcpClient.Available < 1)
            {
                await Task.Delay(100).ConfigureAwait(false);
                continue;
            }

            SocketMode socketIntent = await wrapped.GetSocketMode().ConfigureAwait(false);

            if (socketIntent is SocketMode.RequestIdentify && !identificationSent) // Remote requests your identification!
            {
                await SendIdentify().ConfigureAwait(false);
                identificationSent = true;
            }

            Object? boxedResponse = await wrapped.GetSocketContent(socketIntent, await wrapped.GetSocketContentLength().ConfigureAwait(false)).ConfigureAwait(false);

            if (socketIntent is SocketMode.EncodedMessage)
            {
                string msg = (string)boxedResponse!; //! Message is GUARANTEED to NOT be null
                Console.WriteLine($"{tcpClient.Client.RemoteEndPoint} Sent Message > {msg}");
            }
        }
    }

    internal static async Task SendIdentify()
    {
        if (!tcpClient.Connected)
            throw new InvalidOperationException("Can not send Identify packet if there is no connection in place!");

        Console.WriteLine("Please, enter how you want the server to identify you.");
        identification = new()
        {
            PeerName = Console.ReadLine()!,
            OpenPort = int.Parse(tcpClient.Client.LocalEndPoint.ToString().Split(":")[^1]), // Get port only.
            // Temporal, Type-Unsafe IPV4 address fetcher, 1993 edition! (Not really.)
            PeerIp = JsonConvert.DeserializeObject<ipv4API>(await new HttpClient().GetStringAsync("https://api.ipify.org/?format=json").ConfigureAwait(false)).ip,
        };

        string jSerializedId = identification.GetSerializedAsJson();
        Console.WriteLine(jSerializedId);
        byte[] data = await GetAsBase64(jSerializedId, Encoding.UTF8).ConfigureAwait(false);
        byte[] dataSizeAsLong = BitConverter.GetBytes(data.LongLength);
        Console.WriteLine("Json is " + data.LongLength + " bytes long, encoded like " + BitConverter.ToInt64(dataSizeAsLong));
        // Send SendIdentify Packet.
        netStream.WriteByte((byte)SocketMode.SendIdentify); // Send intent.
        await netStream.FlushAsync().ConfigureAwait(false); // Flush

        await netStream.WriteAsync(dataSizeAsLong).ConfigureAwait(false); // Send data length

        await netStream.WriteAsync(data).ConfigureAwait(false); // Send data.
        await netStream.FlushAsync().ConfigureAwait(false); // Flush
    }

    public static async Task SendMessage()
    {
        if (!tcpClient.Connected)
            throw new InvalidOperationException("Can not send EncodedMessage SocketMode if there is no connection in place!");

        tcpClient.ReceiveBufferSize = Constants.BUF_SIZE;

        while (true)
        {
            if (!identificationSent)
            {
                Console.WriteLine("Please, Identify yourself before sending messages.");

                while (!identificationSent)
                {
                    await Task.Delay(200);
                }
            }

            string? msg = null;
            Console.Write($"Send to {tcpClient.Client.RemoteEndPoint} > ");

            while (msg is null)
                msg = Console.ReadLine();

            Console.WriteLine($"Sending message to {tcpClient.Client.RemoteEndPoint}...");

            netStream.WriteByte((byte)SocketMode.EncodedMessage); // Send socket mode.
            await netStream.FlushAsync().ConfigureAwait(false); // Flush.

            byte[] data = await GetAsBase64(msg, Encoding.UTF8).ConfigureAwait(false);
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
    /// <summary>
    /// Get a byte[] that represents the string with the specified encoding encoded into Base64.
    /// </summary>
    /// <param name="str">String to be converted</param>
    /// <param name="encoding">The string encoding</param>
    /// <returns>A Byte[] that represents the string as a byte[]</returns>
    private static async Task<byte[]> GetAsBase64(string str, Encoding encoding)
        => encoding.GetBytes(Convert.ToBase64String(encoding.GetBytes(str)));

}