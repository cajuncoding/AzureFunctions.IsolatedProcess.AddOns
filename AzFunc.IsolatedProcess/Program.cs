using System.IO.Compression;
using AzFunc.IsolatedProcess;
using Functions.Worker.ContextAccessor;
using Functions.Worker.HttpResponseDataCompression;
using Functions.Worker.HttpResponseDataJsonMiddleware;
using Functions.Worker.ILoggerSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host
    .CreateDefaultBuilder()
    .ConfigureFunctionsWorkerDefaults(app =>
    {
        app
            .UseFunctionContextAccessor()
            .UseHttpResponseDataCompression()
            .UseJsonResponses();
    })
    .ConfigureServices(svc =>
    {
        svc
            .AddFunctionILoggerSupport()
            .ConfigureHttpResponseDataCompression(opt =>
            {
                opt.GzipCompressionLevel = CompressionLevel.SmallestSize;
                opt.BrotliCompressionLevel= CompressionLevel.Fastest;
                opt.DeflateCompressionLevel = CompressionLevel.SmallestSize;
            })
            .AddSingleton<TestILoggerSingleton>()
            .AddScoped<TestILoggerScoped>();
    })
    .Build();

await host.RunAsync().ConfigureAwait(false);
