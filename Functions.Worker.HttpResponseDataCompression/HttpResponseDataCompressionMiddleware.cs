#nullable enable
using System;
using System.IO.Compression;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Options;

namespace Functions.Worker.HttpResponseDataCompression
{
    /// <summary>
    /// BBernard / CajunCoding (MIT License)
    /// Middleware for Azure Functions Worker (Isolated Process) to enable response compression within Azure Functions using the
    ///     HttpResponseData (e.g. plain vanilla Azure Function Isolated Process).
    /// This is not intended to be used in combination with Asp.Net Core integrated model which more likely should use the existing Asp.Net Core Middleware,
    ///     which does not work when using HttpResponseData.
    /// It works by simply inspecting the AcceptEncoding header to determine which, if any, compression encodings are supported (Gzip, Brotli, Deflate)
    ///     and then buffering and compressing the Response Body Stream with the correct implementation to encode the response while also setting the correct Response Header
    ///     for the Client to correctly decode the response.
    /// NOTE: Unfortunately the Azure Functions Isolated Process doesn't support full streaming (due to the GRPC Remove invocation and marshalling of results in/out of hte process (e.g. as strings)
    ///     therefore the response body must be buffered in memory and copied to be compressed and returned. But we make every effort to dispose of, and release, resources as quickly as possible
    /// NOTE: There is supposed to be a Microsoft provided solution, but it does not seem to work with vanilla HttpResponseData, and there is
    ///     little to no documentation on how to get it to work at all. I tried quite unsuccessfully, and therefore published this which can
    ///     be up and running very easily.  More Info here: https://github.com/Azure/azure-functions-host/pull/10870
    /// NOTE: *** IMPORTANT!
    ///       *** You MUST handle the response writing manually to support the Compression; you cannot rely on returning
    ///       ***  an object, or HttpMessageResult, etc... If used in those use cases, then the easiest way is to simply
    ///       *** write to the HttpResponseData directly and then return an IActionResult of new EmptyResult().
    /// </summary>
    public class HttpResponseDataCompressionMiddleware : IFunctionsWorkerMiddleware
    {
        public HttpResponseDataCompressionMiddleware(IOptions<HttpResponseDataCompressionOptions>? options = null)
        {
            Options = options?.Value ?? new HttpResponseDataCompressionOptions();
        }

        public HttpResponseDataCompressionOptions Options { get; }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            await next(context);

            var httpRequestData = await context.GetHttpRequestDataAsync().ConfigureAwait(false);
            if (httpRequestData is not null
                && httpRequestData.Headers.TryGetValues(CompressionHeaderNames.AcceptEncoding, out var acceptHeader)
                && context.GetHttpResponseData() is { } httpResponseData
            ) {
                var acceptHashSet = acceptHeader.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var compressedStream = new MemoryStream(); //✅ Compressed (output) stream is NOT disposed because it's assigned to the HttpResponseData to be handled by the Framework...
                var (compressionHandlerStream, compressionTypeName) = acceptHashSet switch
                {
                    //NOTE: We MUST leave the base/output stream Open because we use it to replace the Response Body stream...
                    //NOTE: We prioritize Brotli so this is consistent with what Microsoft states in various GitHub issues...
                    _ when acceptHashSet.Contains(AcceptCompressionNames.Brotli) => (new BrotliStream(compressedStream, Options.BrotliCompressionLevel, leaveOpen: true), AcceptCompressionNames.Brotli),
                    _ when acceptHashSet.Contains(AcceptCompressionNames.Gzip) => (new GZipStream(compressedStream, Options.GzipCompressionLevel, leaveOpen: true), AcceptCompressionNames.Gzip),
                    _ when acceptHashSet.Contains(AcceptCompressionNames.Deflate) => (new DeflateStream(compressedStream, Options.DeflateCompressionLevel, leaveOpen: true), AcceptCompressionNames.Deflate),
                    _ => (Stream.Null, string.Empty)
                };

                if (compressionHandlerStream != Stream.Null)
                {
                    await using (var responseBufferStream = httpResponseData.Body) //✅ Original body stream will be Disposed
                    {
                        responseBufferStream.Seek(0, SeekOrigin.Begin);
                        await using (compressionHandlerStream) //✅ Compression Handler stream will be Disposed
                        {
                            responseBufferStream.Seek(0, SeekOrigin.Begin);
                            await responseBufferStream.CopyToAsync(compressionHandlerStream).ConfigureAwait(false);
                        }
                    }

                    // Replace the response body with the compressed content
                    httpResponseData.Headers.Add(CompressionHeaderNames.ContentEncoding, compressionTypeName);
                    httpResponseData.Body = compressedStream;
                }
            }
        }
    }
}
