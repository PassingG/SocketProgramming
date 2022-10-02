using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerCore
{
    class Session
    {
        private Socket _socket;
        private int _disconnected = 0;

        public void Start(Socket socket)
        {
            _socket = socket;
            SocketAsyncEventArgs receiveArgs = new SocketAsyncEventArgs();
            receiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceiveCompleted);
            receiveArgs.SetBuffer(new byte[1024], 0, 1024);

            RegisterReceive(receiveArgs);
        }

        public void Send(byte[] sendBuffer)
        {
            _socket.Send(sendBuffer);
        }

        public void Disconnect()
        {
            if (Interlocked.Exchange(ref _disconnected, 1) == 1)
            {
                return;
            }

            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }

        #region [ Network Communication ]
        private void RegisterReceive(SocketAsyncEventArgs args)
        {
            bool pending = _socket.ReceiveAsync(args);
            if (pending.Equals(false))
            {
                OnReceiveCompleted(null, args);
            }

            _socket.ReceiveAsync(args);
        }

        private void OnReceiveCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > 0 && args.SocketError.Equals(SocketError.Success))
            {
                try
                {
                    string receiveData = Encoding.UTF8.GetString(args.Buffer, args.Offset, args.BytesTransferred);
                    Console.WriteLine($"[From Client] {receiveData}");

                    RegisterReceive(args);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"OnReceiveCompleted Failed {e}");
                }
            }
            else
            {

            }
        }
        #endregion
    }
}