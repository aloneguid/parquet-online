using Microsoft.AspNetCore.Components.Forms;
using System.Threading;

namespace ParquetViewer.WebAdapters
{
    /// <summary>
    /// Adopted from https://github.com/dotnet/aspnetcore/issues/38785.
    /// Allows seeking in browser stream which is not supported out of the box.
    /// </summary>
    public class BlazorFileStream : Stream
    {
        const int MAX_SEEK_BUFFER_SIZE = 20 * 1024 * 1024;

        IBrowserFile m_file;
        Stream m_browserStream;
        long m_maxAllowedSize;
        int m_seekBufferSize;
        long? m_newPosition;

        public override bool CanRead => m_browserStream.CanRead;

        public override bool CanSeek => true;

        public override bool CanWrite => m_browserStream.CanWrite;

        public override long Length => m_browserStream.Length;

        public override long Position
        {
            get => m_newPosition ?? m_browserStream.Position;
            set => Seek(value, SeekOrigin.Begin);
        }

        public BlazorFileStream(
            IBrowserFile file,
            long maxAllowedSize,
            int seekBufferSize = MAX_SEEK_BUFFER_SIZE)
        {
            m_file = file;
            m_seekBufferSize = seekBufferSize;
            m_maxAllowedSize = maxAllowedSize < m_seekBufferSize ?
                seekBufferSize :
                maxAllowedSize;
            m_browserStream = file.OpenReadStream(m_maxAllowedSize);
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return m_browserStream.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count).Result;
        }

        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            if (m_newPosition != null)
            {
                long seekForwardCount = 0;

                if (m_newPosition > m_browserStream.Position)
                {
                    // Seek forward.
                    seekForwardCount = m_newPosition.Value - m_browserStream.Position;
                }
                else if (m_newPosition < m_browserStream.Position)
                {
                    // Seek backward.
                    // Reopen new stream.
                    m_browserStream.Close();
                    m_browserStream = m_file.OpenReadStream(m_maxAllowedSize, cancellationToken);
                    seekForwardCount = m_newPosition.Value;
                }

                if (seekForwardCount > 0)
                {
                    // Applying forward position.
                    byte[] seekBuff = new byte[m_seekBufferSize];
                    long seekCount = seekForwardCount / m_seekBufferSize;

                    for (int i = 0; i < seekCount; ++i)
                    {
                        int read = await m_browserStream.ReadAsync(
                            seekBuff,
                            0,
                            m_seekBufferSize,
                            cancellationToken);
                        seekForwardCount -= read;
                    }

                    while (seekForwardCount > 0)
                    {
                        int read = await m_browserStream.ReadAsync(
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
            return await m_browserStream.ReadAsync(
                buffer,
                offset,
                count,
                cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long posAfterSeek = 0;

            if (origin == SeekOrigin.Begin)
            {
                posAfterSeek = offset;
            }
            else if (origin == SeekOrigin.Current)
            {
                posAfterSeek = Position + offset;
            }
            else
            {
                posAfterSeek = Length + offset;
            }

            if (posAfterSeek < 0 || posAfterSeek > Length)
            {
                throw new InvalidOperationException("Invalid position");
            }

            m_newPosition = posAfterSeek;
            return m_newPosition.Value;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void Close()
        {
            m_browserStream.Close();
        }
    }
}
