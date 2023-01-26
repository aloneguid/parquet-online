using Microsoft.AspNetCore.Components.Forms;
using System.Threading;

namespace ParquetViewer.WebAdapters {
    /// <summary>
    /// Adopted from https://github.com/dotnet/aspnetcore/issues/38785.
    /// Allows seeking in browser stream which is not supported out of the box.
    /// </summary>
    public class BlazorFileStream : Stream {
        const int MaxSeekbufferSize = 20 * 1024 * 1024;
        readonly IBrowserFile _file;
        Stream _browserStream;
        readonly long _maxAllowedSize;
        readonly int _seekBufferSize;
        long? m_newPosition;

        public override bool CanRead => _browserStream.CanRead;

        public override bool CanSeek => true;

        public override bool CanWrite => _browserStream.CanWrite;

        public override long Length => _browserStream.Length;

        public override long Position {
            get => m_newPosition ?? _browserStream.Position;
            set => Seek(value, SeekOrigin.Begin);
        }

        public BlazorFileStream(
            IBrowserFile file,
            long maxAllowedSize,
            int seekBufferSize = MaxSeekbufferSize) {
            _file = file;
            _seekBufferSize = seekBufferSize;
            _maxAllowedSize = maxAllowedSize < _seekBufferSize ?
                seekBufferSize :
                maxAllowedSize;
            _browserStream = file.OpenReadStream(_maxAllowedSize);
        }

        public override void Flush() {
            throw new NotSupportedException();
        }

        public override Task FlushAsync(CancellationToken cancellationToken) {
            return _browserStream.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count) {
            return ReadAsync(buffer, offset, count).Result;
        }

        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken) {
            if(m_newPosition != null) {
                long seekForwardCount = 0;

                if(m_newPosition > _browserStream.Position) {
                    // Seek forward.
                    seekForwardCount = m_newPosition.Value - _browserStream.Position;
                } else if(m_newPosition < _browserStream.Position) {
                    // Seek backward.
                    // Reopen new stream.
                    _browserStream.Close();
                    _browserStream = _file.OpenReadStream(_maxAllowedSize, cancellationToken);
                    seekForwardCount = m_newPosition.Value;
                }

                if(seekForwardCount > 0) {
                    // Applying forward position.
                    byte[] seekBuff = new byte[_seekBufferSize];
                    long seekCount = seekForwardCount / _seekBufferSize;

                    for(int i = 0; i < seekCount; ++i) {
                        int read = await _browserStream.ReadAsync(
                            seekBuff,
                            0,
                            _seekBufferSize,
                            cancellationToken);
                        seekForwardCount -= read;
                    }

                    while(seekForwardCount > 0) {
                        int read = await _browserStream.ReadAsync(
                            seekBuff,
                            0,
                            (int)seekForwardCount,
                            cancellationToken);
                        seekForwardCount -= read;
                    }
                }

                // Done.
                m_newPosition = null;
            }

            // Read.
            return await _browserStream.ReadAsync(
                buffer,
                offset,
                count,
                cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin) {
            long posAfterSeek = 0;

            if(origin == SeekOrigin.Begin) {
                posAfterSeek = offset;
            } else if(origin == SeekOrigin.Current) {
                posAfterSeek = Position + offset;
            } else {
                posAfterSeek = Length + offset;
            }

            if(posAfterSeek < 0 || posAfterSeek > Length) {
                throw new InvalidOperationException("Invalid position");
            }

            m_newPosition = posAfterSeek;
            return m_newPosition.Value;
        }

        public override void SetLength(long value) {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count) {
            throw new NotSupportedException();
        }

        public override void Close() {
            _browserStream.Close();
        }
    }
}