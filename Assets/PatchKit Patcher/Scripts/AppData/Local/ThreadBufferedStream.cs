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

        private volatile bool _abort;

        private readonly Semaphore _semaphore = new Semaphore(1, 1);

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
            ThreadPool.QueueUserWorkItem((x) =>
            {
                while (!_abort)
                {
                    try
                    {
                        //UnityEngine.Debug.Log("Entering semaphore from thread...");
                        _semaphore.WaitOne();
                        {
                            if (_eof)
                            {
                                //UnityEngine.Debug.Log("BREAKING semaphore from thread...");
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
                        //UnityEngine.Debug.Log("Leaving semaphore from thread.");
                        _semaphore.Release();
                    }

                    Thread.Sleep(1);
                }
            });
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int copied = 0;

            try
            {
                //UnityEngine.Debug.Log("Entering semaphore from Read..." + count);
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
                            //UnityEngine.Debug.Log("BREAK semaphore from Read.");
                            break;
                        }
                    }
                }
            }
            finally
            {
                //UnityEngine.Debug.Log("Leaving semaphore from Read.");
                _semaphore.Release();
            }

            _position += copied;

            //UnityEngine.Debug.Log("Returning " + copied);

            return copied;
        }

        private void ReadToBuffer()
        {
            int size = _buffer.Length - _bufferedBytes;
            //UnityEngine.Debug.Log("Reading into buffer " + size);

            int read = _innerStream.Read(_buffer, _bufferedBytes, size);
            _bufferedBytes += read;

            if (read == 0)
            {
                //UnityEngine.Debug.Log("End of file");
                _eof = true;
            }
        }

        private void ShiftLeftArray(byte[] buffer, int offset)
        {
            Buffer.BlockCopy(
                src: buffer,
                srcOffset: offset,
                dst: buffer,
                dstOffset: 0,
                count: buffer.Length - offset);
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
        }
    }
}