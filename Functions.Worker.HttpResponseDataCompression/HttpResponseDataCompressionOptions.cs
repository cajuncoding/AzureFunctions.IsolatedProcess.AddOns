using System.IO.Compression;

namespace Functions.Worker.HttpResponseDataCompression
{
    public class HttpResponseDataCompressionOptions
    {
        ///<summary>Sets the compression level of Gzip compression, when used; defaults to CompressionLevel.Fastest for Gzip.</summary>
        public CompressionLevel GzipCompressionLevel { get; set; } = CompressionLevel.Fastest;
        ///<summary>Sets the compression level of Gzip compression, when used; defaults to CompressionLevel.Optimal for Brotli.</summary>
        public CompressionLevel BrotliCompressionLevel { get; set; } = CompressionLevel.Optimal;
        ///<summary>Sets the compression level of Gzip compression, when used; defaults to CompressionLevel.Optimal for Deflate.</summary>
        public CompressionLevel DeflateCompressionLevel { get; set; } = CompressionLevel.Optimal;
    }
}
