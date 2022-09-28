using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerCore
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

            // Make like GateKeeper's Phone
            Socket listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                // Teach GateKeeper
                listenSocket.Bind(endPoint);

                // Start market
                // backlog : max delay count
                listenSocket.Listen(10);

                while (true)
                {
                    Console.WriteLine("Listening...");

                    // To let a guest in.
                    Socket clientSocket = listenSocket.Accept();

                    // Receive
                    byte[] receiveBuffer = new byte[1024];
                    int bufferSize = clientSocket.Receive(receiveBuffer);
                    string receiveData = Encoding.UTF8.GetString(receiveBuffer, 0, bufferSize);
                    Console.WriteLine($"[From Client] {receiveData}");

                    // Send
                    byte[] sendBuffer = Encoding.UTF8.GetBytes("Welcome to MMORPG Server !");
                    clientSocket.Send(sendBuffer);

                    // Get Out
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}