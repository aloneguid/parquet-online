using KristofferStrube.Blazor.FileSystem;
using Parquet.Rows;
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
        }

        public static bool HasFile { get; private set; }

        public static string? FileName { get; private set; }

        public static long? FileSize { get; private set; }

        public static Stream? RandomAccessStream { get; private set; }

        public static Table? Table { get; set; }

        public static ParquetSchema? ManagedSchema { get; set; }

        public static FileMetaData? ThriftMetadata { get; set; }

        public static async Task Clear() {
            FileName = null;
            FileSize = 0;
            if(RandomAccessStream != null) {
                await RandomAccessStream.DisposeAsync();
            }
            Table = null;
            ManagedSchema = null;
            ThriftMetadata = null;
        }
    }
}
