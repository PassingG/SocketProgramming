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

        private List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();

        private SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
        private SocketAsyncEventArgs _receiveArgs = new SocketAsyncEventArgs();

        public void OnConnected(EndPoint endPoint) { }
        public void OnReceive(ArraySegment<byte> buffer) { }
        public void OnSend(int numberOfBytes) { }
        public void OnDisconnected(EndPoint endPoint) { }

        public void Start(Socket socket)
        {
            _socket = socket;
            _receiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceiveCompleted);
            _receiveArgs.SetBuffer(new byte[1024], 0, 1024);

            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

            RegisterReceive();
        }

        public void Send(byte[] sendBuffer)
        {
            lock (_lock)
            {
                _sendQueue.Enqueue(sendBuffer);
                if (_pendingList.Count.Equals(0))
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
            while (_sendQueue.Count > 0)
            {
                byte[] buffer = _sendQueue.Dequeue();
                _pendingList.Add(new ArraySegment<byte>(buffer, 0, buffer.Length));
            }
            _sendArgs.BufferList = _pendingList;

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
                        _sendArgs.BufferList = null;
                        _pendingList.Clear();

                        Console.WriteLine($"Transferred bytes: {_sendArgs.BytesTransferred}");

                        if (_sendQueue.Count > 0)
                        {
                            RegisterSend();
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

        private void RegisterReceive()
        {
            bool pending = _socket.ReceiveAsync(_receiveArgs);
            if (pending.Equals(false))
            {
                OnReceiveCompleted(null, _receiveArgs);
            }

            _socket.ReceiveAsync(_receiveArgs);
        }

        private void OnReceiveCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > 0 && args.SocketError.Equals(SocketError.Success))
            {
                try
                {
                    string receiveData = Encoding.UTF8.GetString(args.Buffer, args.Offset, args.BytesTransferred);
                    Console.WriteLine($"[From Client] {receiveData}");

                    RegisterReceive();
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