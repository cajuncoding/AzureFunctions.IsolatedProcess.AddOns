using System.IO.Compression;
using Functions.Worker.ContextAccessor;
using Functions.Worker.HttpResponseDataCompression;
using Functions.Worker.ILoggerSupport;
using Microsoft.Extensions.Hosting;

var host = Host
    .CreateDefaultBuilder()
    .ConfigureFunctionsWorkerDefaults(app =>
    {
        app
            //.UseFunctionContextAccessor()
            .UseHttpResponseDataCompression();
    })
    .ConfigureServices(svc =>
    {
        svc
            //.AddFunctionILoggerSupport()
            .ConfigureHttpResponseDataCompression(opt =>
            {
                opt.GzipCompressionLevel = CompressionLevel.SmallestSize;
                opt.BrotliCompressionLevel= CompressionLevel.Fastest;
                opt.DeflateCompressionLevel = CompressionLevel.SmallestSize;
            });
    })
    .Build();

await host.RunAsync().ConfigureAwait(false);
