using System;
using System.Collections.Generic;
using System.Text;

namespace ServerCore
{
    public class ReceiveBuffer
    {
        private ArraySegment<byte> _buffer;
        private int _readPos;
        private int _writePos;

        public ReceiveBuffer(int bufferSize)
        {
            _buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);
        }

        public int DataSize { get { return _writePos - _readPos; } }
        public int FreeSize { get { return _buffer.Count - _writePos; } }

        public ArraySegment<byte> ReadSegment
        {
            get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _readPos, DataSize); }
        }

        public ArraySegment<byte> WriteSegment
        {
            get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _writePos, FreeSize); }
        }

        public void Clean()
        {
            int dataSize = DataSize;
            if (dataSize == 0)
            {
                // Reset the cursor position without copying if the remaining data does not exist.
                _readPos = _writePos = 0;
            }
            else
            {
                // Copying data from the start location if it remains.
                Array.Copy(_buffer.Array, _buffer.Offset + _readPos, _buffer.Array, _buffer.Offset, dataSize);
                _readPos = 0;
                _writePos = dataSize;
            }
        }

        public bool OnRead(int numberOfBytes)
        {
            if (numberOfBytes > DataSize)
            {
                return false;
            }

            _readPos += numberOfBytes;
            return true;
        }

        public bool OnWrite(int numberOfBytes)
        {
            if (numberOfBytes > FreeSize)
            {
                return false;
            }

            _writePos += numberOfBytes;
            return true;
        }
    }
}