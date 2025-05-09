using Functions.Worker.AddOns.Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace Maestro.BackgroundProcessing.Middleware;

/// <summary>
/// BBernard / CajunCoding (MIT License)
/// Middleware for Azure Functions Worker (Isolated Process) to enable Json response handling of POCO or DTO objects when using only
///     HttpRequestData/HttpResponseData (e.g. plain vanilla Azure Function Isolated Process).
/// This provides easy addition of automatic Json response handling and reduces the need for full AspNetCore dependencies for simple
///     APIs while also minimizing hte need to handle HttpResponseData manually in every function.
/// This can be used in combination with the Functions.Worker.HttpResponseDataCompression (separate Nuget package) when added after the compression middleware is added.
/// It works by handling the response of any Function that has an HttpTrigger binding, intercepting the invocation result and automatically serializing to Json anytime
///     the result is not an HttpResponseData; thereby enabling full manual control anytime you want by returning the low level HttpResponseData.
///     Otherwise, anytime a data model (POCO/DTO) is returned from the Function, then it will be rendered out as proper Json along with proper Content-Type and Encoding headers.
/// </summary>
public class HttpResponseDataJsonMiddleware : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        await next(context);

        // Only process if this is an HTTP Trigger function
        //NOTE: You must have a valid HttpRequestData injected into the Function for any bindings to be available!
        if (context.IsHttpTriggerFunction())
        {
            var invocationResult = context.GetInvocationResult();
            if (invocationResult.Value is not HttpResponseData)
            {
                // Convert the response object to JSON
                //NOTE: If HttpRequestData was not injected into the Function there will be no binding so we can't create a response (e.g. it will be null)...
                var httpResponseData = await context.GetOrCreateHttpResponseDataAsync().ConfigureAwait(false);
                if (httpResponseData is not null)
                {
                    await httpResponseData.WriteAsJsonAsync(invocationResult.Value).ConfigureAwait(false);
                    invocationResult.Value = httpResponseData;
                }
                else
                {
                    context.LogWarning(
                        "Unable to create HttpResponseData for Function [{FunctionName}] because no HttpRequestData was injected into the Function. " +
                        "This is required to create a valid HttpDataResponse. The function may or may not return the result in expected Json format but it will likely not have correct Json Content-Type encoding.",
                        context.FunctionDefinition.Name
                    );
                }
            }
        }
    }
}