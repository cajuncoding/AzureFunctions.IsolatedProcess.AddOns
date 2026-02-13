# AzureFunctions.IsolatedProcess.AddOns
A repository of various add-ons for Azure Functions Isolated Process (e.g. Worker AddOns)

Most are published as independent Nuget Packages for buffet style inclusion of whichever add-ons you need.

### [Buy me a Coffee ☕](https://www.buymeacoffee.com/cajuncoding)
*I'm happy to share with the community, but if you find this useful (e.g for professional use), and are so inclinded,
then I do love-me-some-coffee!*

<a href="https://www.buymeacoffee.com/cajuncoding" target="_blank">
<img src="https://cdn.buymeacoffee.com/buttons/default-orange.png" alt="Buy Me A Coffee" height="41" width="174">
</a> 


## Functions.Worker.ILoggerSupport
Easily add ILogger (non-generic) support logging back into Azure Functions (Isolated Process) for improved DI, 
better de-coupling from generic types, improved code portability, etc.

This is dependent on having valid DI support for the `FunctionContext` which is provided by the handy package [`Functions.Worker.ContextAccessor`](https://github.com/benrobot/Functions.Worker.ContextAccessor).

### Nuget Package
-  [Functions.Worker.ILoggerSupport NuGet package](https://www.nuget.org/packages/Functions.Worker.ILoggerSupport/)

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
#### If your logs don't show in the console or Portal Stream...
If your logs are not showing in the console (locally) or portal stream it may likely be due to some recent changes in the isolated process model Host functionality.
It seems that some changes to the internal bits & bobs has now made it more strict and the standard `"Function": "Information"` line in your `host.json` may no
longer be sufficient. Luckily the fix is easy, but unfortunately it may require long term upkeep if you rename your Functions.

You now need to add explicit log level lines for each function in the format `"Function.{{FunctionNameFromFunctionAttribute}}": "Information"` (or whatever log level you like for that Function.

So for the sample program in this GitHub we now have to add `"Function.FunctionTestILogger": "Information"` to the `host.json` which now looks like:
```json
{
  "version": "2.0",
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true,
        "excludedTypes": "Request"
      },
      "enableLiveMetricsFilters": true
    },
    "logLevel": {
      "default": "Warning",
      "Host.Results": "Information",
      "Function": "Information",
      "Function.FunctionTestILogger": "Information"
    }
  }
}
```
It's annoying to have to upkeep this, but far less of a deal breaker for the DI Friendliness that a working ILogger provides to all of your application layers!

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

### Nuget Package
-  [Functions.Worker.HttpResponseDataCompression NuGet package](https://www.nuget.org/packages/Functions.Worker.HttpResponseDataCompression/)

### Usage

Add to your startup process in the `Program.cs` file:
```csharp
//Required so that the HttpResponseData Compression Middleware is enabled!
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

## Functions.Worker.HttpResponseDataJsonMiddleware
Easily add response Json response handling for POCO or DTO objects from plain vanilla Isolated Process Azure Functions. 
This reduces the need for full AspNetCore dependencies for simple APIs while also minimizing hte need to handle HttpResponseData manually in every function.

This provides a middleware for Azure Functions Worker (Isolated Process) to enable Json response handling of POCO or DTO objects when using only
HttpRequestData/HttpResponseData (e.g. plain vanilla Azure Function Isolated Process).

By enabling the easy addition of automatic Json response handling we reduces the need for full AspNetCore dependencies for simple APIs while 
also minimizing hte need to handle HttpResponseData manually in every function. 

In addition, this can be used in combination with the Functions.Worker.HttpResponseDataCompression (separate Nuget package) when added after the compression middleware is added.

It works by handling the response of any Function that has an HttpTrigger binding, intercepting the invocation result and automatically serializing to Json anytime
the result is not an HttpResponseData; thereby enabling full manual control anytime you want by returning the low level HttpResponseData.
Otherwise, anytime a data model (POCO/DTO) is returned from the Function, then it will be rendered out as proper Json along with proper Content-Type and Encoding headers.

NOTES: 
 - The Azure Functions Isolated Process does handle POCO/DTO object responses unfortunately it does so awkwardly in that it encodes them as `text/plain` responses.
 This violates good practices for a Json API so unfortunately we have to manually account for this behavior.

### Nuget Package
-  [Functions.Worker.HttpResponseDataJsonMiddleware NuGet package](https://www.nuget.org/packages/Functions.Worker.HttpResponseDataJsonMiddleware/)

### Usage

Add to your startup process in the `Program.cs` file:
```csharp
//Required so that the Json Response Middleware is enabled!
.ConfigureFunctionsWorkerDefaults(app => app.UseJsonResponses())
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
        //To use in combination with the Functions.Worker.HttpResponseDataCompression
        //  simply initialize the compression middleware first...
        app.UseHttpResponseDataCompression();
        //Then add the Json response middleware...
        app.UseJsonResponses();
    })
    .Build();

await host.RunAsync().ConfigureAwait(false);
```
