using System.IO.Compression;
using AzFunc.IsolatedProcess;
using Functions.Worker.ContextAccessor;
using Functions.Worker.HttpResponseDataCompression;
using Functions.Worker.HttpResponseDataJsonMiddleware;
using Functions.Worker.ILoggerSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;

var host = Host
    .CreateDefaultBuilder()
    .ConfigureFunctionsWorkerDefaults(app =>
    {
        app
            .UseFunctionContextAccessor()
            .UseHttpResponseDataCompression()
            //.UseJsonResponses();
            .UseJsonResponses((exc) => exc switch
            {
                //We simply return the Exceptions to allow the JsonMiddleware to automitically convert the Exceptions to a standardized Json friendly format...
                //Otherwise you can return any error model you like here and it'll be handled as an error with the specified HttpStatusCode.
                FormatException => (HttpStatusCode.BadRequest, exc),
                InvalidOperationException => (HttpStatusCode.Conflict, exc),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, exc),
                _ => (HttpStatusCode.InternalServerError, exc)
            });
    })
    .ConfigureServices(svc =>
    {
        svc
            .AddFunctionILoggerSupport()
            .ConfigureHttpResponseDataCompression(opt =>
            {
                opt.GzipCompressionLevel = CompressionLevel.SmallestSize;
                opt.BrotliCompressionLevel = CompressionLevel.Fastest;
                opt.DeflateCompressionLevel = CompressionLevel.SmallestSize;
            })
            .AddSingleton<TestILoggerSingleton>()
            .AddScoped<TestILoggerScoped>();
    })
    .Build();

await host.RunAsync().ConfigureAwait(false);
