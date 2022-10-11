using System;
using System.Net;
using System.Text;
using ServerCore;

namespace Server
{
    public class Packet
    {
        public ushort size;
        public ushort packetId;
    }

    class GameSession : PacketSession
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected : {endPoint}");

            // Packet knight = new Packet() { size = 100, packetId = 10 };

            // ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
            // byte[] buffer = BitConverter.GetBytes(knight.hp);
            // byte[] buffer2 = BitConverter.GetBytes(knight.attack);
            // Array.Copy(buffer, 0, openSegment.Array, openSegment.Offset, buffer.Length);
            // Array.Copy(buffer2, 0, openSegment.Array, openSegment.Offset + buffer.Length, buffer2.Length);
            // ArraySegment<byte> sendBuffer = SendBufferHelper.Close(buffer.Length + buffer2.Length);

            // Send(sendBuffer);
            Thread.Sleep(5000);
            Disconnect();
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisconnected : {endPoint}");
        }

        public override void OnReceivePacket(ArraySegment<byte> buffer)
        {
            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + 2);
            Console.WriteLine($"ReceivePacketId: {id}, Size: {size}");
        }

        public override void OnSend(int numberOfBytes)
        {
            Console.WriteLine($"Transferred bytes: {numberOfBytes}");
        }
    }

    class Program
    {
        static Listener _listener = new Listener();

        static void Main(string[] args)
        {
            // DNS (Domain Name System)
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddress = ipHost.AddressList[1];
            IPEndPoint endPoint = new IPEndPoint(ipAddress, 7777);

            _listener.Init(endPoint, () => { return new GameSession(); });

            DateTime before = DateTime.Now;

            while (true)
            {
                // if ((DateTime.Now - before).TotalSeconds > 1)
                // {
                //     Console.WriteLine("aa");

                //     before = DateTime.Now;
                // }
            }
        }
    }
}