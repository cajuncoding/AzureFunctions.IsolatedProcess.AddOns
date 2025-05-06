# AzureFunctions.IsolatedProcess.AddOns
A repository of various add-ons for Azure Functions Isolated Process (e.g. Worker AddOns)

Most are published as independent Nuget Packages for buffet style inclusion of whichever add-ons you need.

## Functions.Worker.ILoggerSupport
Easily add ILogger (non-generic) support logging back into Azure Functions (Isolated Process) for improved DI, 
better de-coupling from generic types, improved code portability, etc.

This is dependent on having valid DI support for the `FunctionContext` which is provided by the handy package [`Functions.Worker.ContextAccessor`](https://github.com/benrobot/Functions.Worker.ContextAccessor).

### Usage

Add to your startup process in the `Program.cs` file:
```csharp
//Required so that the FunctionContextAccessor Middleware is enabled!
.ConfigureFunctionsWorkerDefaults(app => app.UseFunctionContextAccessor())
```
and then you may simply add functionality....
```csharp
//This will automatically call .AddFunctionContextAccessor() for you...
.ConfigureServices(services => services.AddFunctionILoggerSupport())
```

Full example of the startup process in `Program.cs`:
```csharp
using Microsoft.Extensions.Hosting;
using Functions.Worker.ContextAccessor;
using Functions.Worker.ILoggerSupport;

var host = Host
    .CreateDefaultBuilder()
    .ConfigureFunctionsWorkerDefaults(app =>
    {
        //MUST be called for FunctionContextAccessor to be available!
        app.UseFunctionContextAccessor();
    })
    .ConfigureServices(svc =>
    {
        svc.AddFunctionILoggerSupport();
    })
    .Build();

await host.RunAsync().ConfigureAwait(false);
```

## Functions.Worker.HttpResponseDataCompression
Easily add response compression support, via middleware, for Azure Functions (Isolated Process) when using HttpResponseData.

This provides the correct middleware for Azure Functions Worker (Isolated Process) to enable response compression within Azure Functions using the
HttpResponseData (e.g. plain vanilla Azure Function Isolated Process).

It works by simply inspecting the AcceptEncoding header to determine which, if any, compression encodings are supported (Gzip, Brotli, Deflate)
and then buffering and compressing the Response Body Stream with the correct implementation to encode the response while also setting the correct Response Header
for the Client to correctly decode the response.

NOTES: 
 - Unfortunately the Azure Functions Isolated Process doesn't support full streaming (due to the GRPC Remove invocation and marshalling of results in/out of hte process (e.g. as strings)
   therefore the response body must be buffered in memory and copied to be compressed and returned. But we make every effort to dispose of, and release, resources as quickly as possible
 - There is supposed to be a Microsoft provided solution, but it does not seem to work with vanilla HttpResponseData, and there is
   little to no documentation on how to get it to work at all. I tried quite unsuccessfully, and therefore published this which can
   be up and running very easily.  More Info here: https://github.com/Azure/azure-functions-host/pull/10870
 - This is not intended to be used in combination with Asp.Net Core integrated model which should likely use the existing Asp.Net Core Middleware.
   - But the Asp.Net Core middleware does not work when using HttpResponseData hence this middleware is necessary.

### Usage

Add to your startup process in the `Program.cs` file:
```csharp
//Required so that the FunctionContextAccessor Middleware is enabled!
.ConfigureFunctionsWorkerDefaults(app => app.UseHttpResponseDataCompression())
```
and then you may optionally configure the compression levels for each type....
```csharp
//This will automatically call .AddFunctionContextAccessor() for you...
.ConfigureServices(svc => {
    svc.ConfigureHttpResponseDataCompression(opt =>
    {
        opt.GzipCompressionLevel = CompressionLevel.SmallestSize;
        opt.BrotliCompressionLevel= CompressionLevel.Fastest;
        opt.DeflateCompressionLevel = CompressionLevel.SmallestSize;
    });
})
```

Full example of the startup process in `Program.cs`:
```csharp
using Microsoft.Extensions.Hosting;
using System.IO.Compression;
using Functions.Worker.HttpResponseDataCompression;

var host = Host
    .CreateDefaultBuilder()
    .ConfigureFunctionsWorkerDefaults(app =>
    {
        app.UseHttpResponseDataCompression();
    })
    .ConfigureServices(svc =>
    {
        svc.ConfigureHttpResponseDataCompression(opt =>
        {
            opt.GzipCompressionLevel = CompressionLevel.SmallestSize;
            opt.BrotliCompressionLevel= CompressionLevel.Fastest;
            opt.DeflateCompressionLevel = CompressionLevel.SmallestSize;
        });
    })
    .Build();

await host.RunAsync().ConfigureAwait(false);
```

