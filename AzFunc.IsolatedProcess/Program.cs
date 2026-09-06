using AzFunc.IsolatedProcess;
using Azure.Core.Serialization;
using Functions.Worker.AddOns.MiniApiRouting;
using Functions.Worker.ContextAccessor;
using Functions.Worker.HttpResponseDataCompression;
using Functions.Worker.HttpResponseDataJsonMiddleware;
using Functions.Worker.ILoggerSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO.Compression;
using System.Net;
using System.Text.Json;

var host = Host
    .CreateDefaultBuilder()
    .ConfigureFunctionsWorkerDefaults(
        app =>
        {
            app
                .UseFunctionContextAccessor()
                .UseHttpResponseDataCompression()
                //.UseJsonResponses();
                .UseJsonResponses(exc => exc switch
                {
                    //We simply return the Exceptions to allow the JsonMiddleware to automitically convert the Exceptions to a standardized Json friendly format...
                    //Otherwise you can return any error model you like here and it'll be handled as an error with the specified HttpStatusCode.
                    FormatException => (HttpStatusCode.BadRequest, exc),
                    InvalidOperationException => (HttpStatusCode.Conflict, exc),
                    UnauthorizedAccessException => (HttpStatusCode.Unauthorized, exc),
                    _ => (HttpStatusCode.InternalServerError, exc)
                });
        },
        configureOptions: worker =>
        {
            //Configure the Azure Function Worker (built-in) Json Serializer to use camelCase for consistency across the board!
            worker.Serializer = new JsonObjectSerializer(new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
        }
    )
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
            .AddFunctionsMiniApiRouting()
            .AddSingleton<TestILoggerSingleton>()
            .AddScoped<TestILoggerScoped>();
    })
    .Build();

await host.RunAsync().ConfigureAwait(false);
