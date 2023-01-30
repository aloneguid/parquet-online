using Parquet;
using Parquet.Schema;
using Parquet.Thrift;
using ParquetViewer.WebAdapters;
using JFile = KristofferStrube.Blazor.FileAPI.File;

namespace ParquetViewer {

    /// <summary>
    /// Holds open file so it can be used accross the app in all the pages
    /// </summary>
    public static class GFile {

        private static FileApiRandomAccessReadableStream? _stream;
        
        public static async Task InitFile(JFile file) {
            if(_stream != null) {
                await _stream.DisposeAsync();
            }
            _stream = await FileApiRandomAccessReadableStream.CreateAsync(file);
            RandomAccessStream = _stream;

            FileName = await file.GetNameAsync();
            FileSize = (long)(await file.GetSizeAsync());

            HasFile = true;

            Console.WriteLine($"opened {FileName} ({FileSize})");

            using(ParquetReader reader = await ParquetReader.CreateAsync(_stream)) {
                ManagedSchema = reader.Schema;
                ThriftMetadata = reader.ThriftMetadata;
            }

            OnFileLoaded?.Invoke(FileName);
        }

        public static bool HasFile { get; private set; }

        public static string? FileName { get; private set; }

        public static long? FileSize { get; private set; }

        public static Stream? RandomAccessStream { get; private set; }

        public static ParquetSchema? ManagedSchema { get; set; }

        public static FileMetaData? ThriftMetadata { get; set; }

        public static event Action<string?>? OnFileLoaded;

        public static async Task Clear() {
            FileName = null;
            FileSize = 0;
            HasFile = false;
            if(RandomAccessStream != null) {
                await RandomAccessStream.DisposeAsync();
            }
            ManagedSchema = null;
            ThriftMetadata = null;
            OnFileLoaded?.Invoke(null);
        }
    }
}
