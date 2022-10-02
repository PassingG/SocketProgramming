using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerCore
{
    class Session
    {
        private Socket _socket;
        private int _disconnected = 0;

        private object _lock = new object();

        private Queue<byte[]> _sendQueue = new Queue<byte[]>();
        private bool _pending = false;

        private SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();

        public void Start(Socket socket)
        {
            _socket = socket;
            SocketAsyncEventArgs receiveArgs = new SocketAsyncEventArgs();
            receiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceiveCompleted);
            receiveArgs.SetBuffer(new byte[1024], 0, 1024);

            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

            RegisterReceive(receiveArgs);
        }

        public void Send(byte[] sendBuffer)
        {
            lock (_lock)
            {
                _sendQueue.Enqueue(sendBuffer);
                if (_pending.Equals(false))
                {
                    RegisterSend();
                }
            }
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

        private void RegisterSend()
        {
            _pending = true;

            byte[] buffer = _sendQueue.Dequeue();
            _sendArgs.SetBuffer(buffer, 0, buffer.Length);

            bool pending = _socket.SendAsync(_sendArgs);
            if (pending.Equals(false))
            {
                OnSendCompleted(null, _sendArgs);
            }
        }

        private void OnSendCompleted(object sender, SocketAsyncEventArgs args)
        {
            lock (_lock)
            {
                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
                {
                    try
                    {
                        if (_sendQueue.Count > 0)
                        {
                            RegisterSend();
                        }
                        else
                        {
                            _pending = false;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"OnSendCompleted Failed {e}");
                    }
                }
                else
                {
                    Disconnect();
                }
            }
        }

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