using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ServerCore
{
    public abstract class PacketSession : Session
    {
        public static readonly int HeaderSize = 2;

        public sealed override int OnReceive(ArraySegment<byte> buffer)
        {
            int processLength = 0;

            while (true)
            {
                // At least make sure the header can be parsed.
                if (buffer.Count < HeaderSize)
                {
                    break;
                }

                // Verify that the packet arrived completely.
                ushort dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
                if (buffer.Count < dataSize)
                {
                    break;
                }

                // Packets can be assembled if you've come all the way here.
                OnReceivePacket(new ArraySegment<byte>(buffer.Array, buffer.Offset, dataSize));

                processLength += dataSize;
                buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + dataSize, buffer.Count - dataSize);
            }
            return 0;
        }

        public abstract void OnReceivePacket(ArraySegment<byte> buffer);
    }

    public abstract class Session
    {
        private Socket _socket;
        private int _disconnected = 0;

        private ReceiveBuffer _receiveBuffer = new ReceiveBuffer(1024);

        private object _lock = new object();

        private Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();

        private List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();

        private SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
        private SocketAsyncEventArgs _receiveArgs = new SocketAsyncEventArgs();

        public abstract int OnReceive(ArraySegment<byte> buffer);
        public abstract void OnConnected(EndPoint endPoint);
        public abstract void OnSend(int numberOfBytes);
        public abstract void OnDisconnected(EndPoint endPoint);

        public void Start(Socket socket)
        {
            _socket = socket;
            _receiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceiveCompleted);

            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

            RegisterReceive();
        }

        public void Send(ArraySegment<byte> sendBuffer)
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

            OnDisconnected(_socket.RemoteEndPoint);
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }

        #region [ Network Communication ]

        private void RegisterSend()
        {
            while (_sendQueue.Count > 0)
            {
                ArraySegment<byte> buffer = _sendQueue.Dequeue();
                _pendingList.Add(buffer);
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

                        OnSend(_sendArgs.BytesTransferred);

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
            _receiveBuffer.Clean();
            ArraySegment<byte> segment = _receiveBuffer.WriteSegment;
            _receiveArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

            bool pending = _socket.ReceiveAsync(_receiveArgs);
            if (pending.Equals(false))
            {
                OnReceiveCompleted(null, _receiveArgs);
            }
        }

        private void OnReceiveCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > 0 && args.SocketError.Equals(SocketError.Success))
            {
                try
                {
                    // Move write cursor
                    if (_receiveBuffer.OnWrite(args.BytesTransferred).Equals(false))
                    {
                        Disconnect();
                        return;
                    }

                    // Hand over the data to the content and receive how much it has been processed
                    int processeLength = OnReceive(new ArraySegment<byte>(args.Buffer, args.Offset, args.BytesTransferred));
                    if (processeLength < 0 || _receiveBuffer.DataSize < processeLength)
                    {
                        Disconnect();
                        return;
                    }

                    // Move read cursor
                    if (_receiveBuffer.OnRead(processeLength).Equals(false))
                    {
                        Disconnect();
                        return;
                    }

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