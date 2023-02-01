using KristofferStrube.Blazor.FileAPI;
using KristofferStrube.Blazor.Streams;

namespace ParquetViewer.WebAdapters {
    public class FileApiRandomAccessReadableStream : Stream {
        readonly KristofferStrube.Blazor.FileAPI.File _f;
        private bool _isValid = false;
        private ReadableStream? _rs;
        private Blob? _b;
        private readonly long _length;
        private long _position;
        private int _jsCallCount;
        private const bool Log = false;

        private FileApiRandomAccessReadableStream(KristofferStrube.Blazor.FileAPI.File f, long length) {
            _f = f;
            _length = length;
        }

        public static async Task<FileApiRandomAccessReadableStream> CreateAsync(KristofferStrube.Blazor.FileAPI.File f) {
            // getting length is async operation so we need an async factory method
            ulong length = await f.GetSizeAsync();
            return new FileApiRandomAccessReadableStream(f, (long)length);
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _length;

        public override long Position { get => _position; set => throw new NotSupportedException(); }

        public override void Flush() => throw new NotSupportedException();

        public override Task FlushAsync(CancellationToken cancellationToken) {
            if(_rs != null)
                return _rs.FlushAsync();
            if(Log)
                Console.WriteLine("flush");
            _jsCallCount++;
            return Task.CompletedTask;
        }

        public override int Read(byte[] buffer, int offset, int count) => 
            throw new NotSupportedException("synchronous reads are not supported");

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            if(!_isValid) {
                await CloseAll();
            }

            int read = 0;

            try {
                // create slice
                if(Log) {
                    Console.WriteLine($"slicing to {_position}/{count}");
                }
                _b = await _f.SliceAsync(_position, _position + count);
                _rs = await _b.StreamAsync();
                _jsCallCount++;

                // read slice
                //Console.WriteLine($"reading {offset}/{count}; buf len: {buffer.Length}");
                read = await _rs.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
                _position += read;
                //Console.WriteLine($"read {read}b, pos: {_position}");
            } finally {
                if(Log) {
                    Console.WriteLine("unslicing");
                }
                await CloseAll();
            }

            return read;
        }

        public override long Seek(long offset, SeekOrigin origin) {
            if(Log) {
                Console.WriteLine($"seek: {origin} -> {offset}");
            }
            switch(origin) {
                case SeekOrigin.Begin:
                    _position = offset;
                    break;
                case SeekOrigin.Current:
                    _position += offset;
                    break;
                case SeekOrigin.End:
                    _position = _length + offset;
                    break;
            }
            _isValid = false;
            return _position;
        }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        async Task CloseAll() {
            if(_rs != null) {
                await _rs.DisposeAsync();
                _jsCallCount++;
                _rs = null;
            }

            if(_b != null) {
                await _b.DisposeAsync();
                _jsCallCount++;
                _b = null;
            }
        }

        public override async ValueTask DisposeAsync() {
            await base.DisposeAsync();

            await CloseAll();
        }
    }
}
