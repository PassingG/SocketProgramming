using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DummyClient
{
    class Program
    {
        static void Main(string[] args)
        {
            // DNS (Domain Name System)
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddress = ipHost.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ipAddress, 7777);

            while (true)
            {
                // Set phone setting
                Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    // Send message to GateKeeper
                    socket.Connect(endPoint);
                    Console.WriteLine($"Connected To {socket.RemoteEndPoint.ToString()}");

                    // Send
                    byte[] sendBuff = Encoding.UTF8.GetBytes("Hello, world!");
                    int sendBytes = socket.Send(sendBuff);

                    // Receive
                    byte[] receiveBuff = new byte[1024];
                    int receiveBytes = socket.Receive(receiveBuff);
                    string receiveData = Encoding.UTF8.GetString(receiveBuff, 0, receiveBytes);
                    Console.WriteLine($"[From Server] {receiveData}");

                    // Exit
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                Thread.Sleep(100);
            }
        }
    }
}