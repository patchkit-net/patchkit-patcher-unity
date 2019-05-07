using System;
using System.IO;
using System.Threading;
using JetBrains.Annotations;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    public class ThreadBufferedStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly byte[] _buffer;

        private Thread _readerThread;

        private volatile bool _abort;

        private readonly Semaphore _semaphore = new Semaphore(0, 1);

        // Accessed only in lock (_buffer)
        private bool _eof;
        private int _bufferedBytes;

        private long _position;

        public ThreadBufferedStream([NotNull] Stream innerStream, int bufferSize)
        {
            if (innerStream == null)
            {
                throw new ArgumentNullException("innerStream");
            }

            _innerStream = innerStream;
            _buffer = new byte[bufferSize];
            _position = 0;

            SpawnReaderThread();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { return _position; }
            set { throw new InvalidOperationException(); }
        }

        private void SpawnReaderThread()
        {
            _readerThread = new Thread(() =>
            {
                while (!_abort)
                {
                    try
                    {
                        _semaphore.WaitOne();
                        {
                            if (_eof)
                            {
                                break;
                            }

                            if (_bufferedBytes < _buffer.Length)
                            {
                                ReadToBuffer();
                            }
                        }
                    }
                    finally
                    {
                        _semaphore.Release();
                    }

                    Thread.Sleep(1);
                }
            })
            {
                IsBackground = true
            };

            _readerThread.Start();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int copied = 0;

            try
            {
                _semaphore.WaitOne();
                {
                    // repeat while there's something to read
                    while (!_eof || _bufferedBytes > 0)
                    {
                        if (_bufferedBytes == 0)
                        {
                            ReadToBuffer();
                        }

                        // copy bytes, but no more than buffer size
                        int toCopy = Math.Min(_bufferedBytes, count - copied);

                        Buffer.BlockCopy(
                            src: _buffer,
                            srcOffset: 0,
                            dst: buffer,
                            dstOffset: offset + copied,
                            count: toCopy);

                        copied += toCopy;

                        // shift the current buffer
                        ShiftLeftArray(_buffer, toCopy);
                        // and adjust its content size info
                        _bufferedBytes -= toCopy;

                        if (copied == count)
                        {
                            // all bytes has been copied
                            break;
                        }
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }

            _position += copied;

            return copied;
        }

        private void ReadToBuffer()
        {
            int read = _innerStream.Read(_buffer, _bufferedBytes, _buffer.Length - _bufferedBytes);
            _bufferedBytes += read;

            if (read == 0)
            {
                _eof = true;
            }
        }

        private void ShiftLeftArray(byte[] buffer, int offset)
        {
            Buffer.BlockCopy(buffer, offset, buffer, 0, buffer.Length - offset);
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException();
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException();
        }

        public override void Write(byte[] bytes, int offset, int count)
        {
            throw new InvalidOperationException();
        }

        public override void Close()
        {
            base.Close();
            _abort = true;
            _readerThread.Join();
        }
    }
}