using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

var ip = "127.0.0.1";
var port = 80;
TcpListener server = new(IPAddress.Parse(ip), port);

server.Start();
Console.WriteLine($"Server has started on {ip}:{port}, Waiting for a connection…\n");

TcpClient client = server.AcceptTcpClient();
Console.WriteLine("A client connected.");

NetworkStream stream = client.GetStream();

while (true)
{
    while (!stream.DataAvailable) ;
    while (client.Available < 3) ;

    var bytes = new byte[client.Available];
    stream.Read(bytes, 0, client.Available);
    string s = Encoding.UTF8.GetString(bytes);

    if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
    {
        Console.WriteLine($"=====Handshaking from client=====\n{s.Replace("\r\n\r\n", "")}");

        string swk = Regex.Match(s, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
        string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        byte[] swkaSha1 = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
        string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

        byte[] response = Encoding.UTF8.GetBytes(
            "HTTP/1.1 101 Switching Protocols\r\n" +
            "Connection: Upgrade\r\n" +
            "Upgrade: websocket\r\n" +
            "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

        stream.Write(response, 0, response.Length);
    }
    else
    {
        var mask = (bytes[1] & 0b10000000) != 0;
        var offset = 2;
        var msglen = bytes[1] & 0b01111111ul;

        if (msglen == 126)
        {
            msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
            offset = 4;
        }
        else if (msglen == 127)
        {
            msglen = BitConverter.ToUInt64(new byte[] { bytes[9], bytes[8], bytes[7], bytes[6], bytes[5], bytes[4], bytes[3], bytes[2] }, 0);
            offset = 10;
        }

        if (msglen == 0)
        {
            Console.WriteLine("msglen == 0");
        }
        else if (mask)
        {
            var decoded = new byte[msglen];
            var masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
            offset += 4;

            for (ulong i = 0; i < msglen; ++i)
                decoded[i] = (byte)(bytes[(ulong)offset + i] ^ masks[i % 4]);

            string text = Encoding.UTF8.GetString(decoded);
            Console.WriteLine($"Client initial message: {text}.");
            while (client.Available == 0)
            {
                string time = DateTime.Now.ToLongTimeString();
                byte[] mes = CreateFrame(time);
                stream.Write(mes, 0, mes.Length);
                Thread.Sleep(2000);
            }

            Console.WriteLine("A client disconnected.\n");
            client.Close();
            client = server.AcceptTcpClient();
            Console.WriteLine("A client connected.");
            stream = client.GetStream();
        }
        else
        {
            Console.WriteLine("mask bit not set");
        }
    }
}

static byte[] CreateFrame(string mes)
{
    byte[] bytesRaw = Encoding.UTF8.GetBytes(mes);
    var bytesFormatted = new byte[bytesRaw.Length + 10];
    bytesFormatted[0] = 129;

    int indexStartRawData;

    if (bytesRaw.Length <= 125)
    {
        bytesFormatted[1] = (byte)bytesRaw.Length;
        indexStartRawData = 2;
    }
    else if (bytesRaw.Length >= 126 && bytesRaw.Length <= 65535)
    {
        bytesFormatted[1] = 126;
        bytesFormatted[2] = (byte)((bytesRaw.Length >> 8) & 255);
        bytesFormatted[3] = (byte)(bytesRaw.Length & 255);
        indexStartRawData = 4;
    }
    else
    {
        bytesFormatted[1] = 127;
        bytesFormatted[2] = (byte)((bytesRaw.Length >> 56) & 255);
        bytesFormatted[3] = (byte)((bytesRaw.Length >> 48) & 255);
        bytesFormatted[4] = (byte)((bytesRaw.Length >> 40) & 255);
        bytesFormatted[5] = (byte)((bytesRaw.Length >> 32) & 255);
        bytesFormatted[6] = (byte)((bytesRaw.Length >> 24) & 255);
        bytesFormatted[7] = (byte)((bytesRaw.Length >> 16) & 255);
        bytesFormatted[8] = (byte)((bytesRaw.Length >> 8) & 255);
        bytesFormatted[9] = (byte)((bytesRaw.Length) & 255);
        indexStartRawData = 10;
    }
    
    for (var i = 0; i < bytesRaw.Length; i++)
    {
        bytesFormatted[i + indexStartRawData] = bytesRaw[i];
    }

    return bytesFormatted;
}