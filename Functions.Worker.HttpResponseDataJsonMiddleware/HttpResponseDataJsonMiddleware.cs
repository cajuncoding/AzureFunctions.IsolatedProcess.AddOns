using Functions.Worker.AddOns.Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace Functions.Worker.HttpResponseDataJsonMiddleware;

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
        Exception? trappedException = null;
        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (Exception exc)
        {
            context.LogError(exc);
            trappedException = exc;
        }

        //Only process if this is an HTTP Trigger function
        //NOTE: You must have a valid HttpRequestData injected into the Function for any bindings to be available!
        if (context.IsHttpTriggerFunction())
        {
            //If we have an exception then we know there is not a valid response and we need to create the response
            //  from the exception details instead of the invocation result...
            //NOTE: We know it cannot be an HttpResponseData because an Exception would not be thrown!
            if (trappedException is not null)
                await HandleHttpTriggerInvocationExceptionAsync(context, trappedException).ConfigureAwait(false);
            else
                await HandleHttpTriggerInvocationAsync(context).ConfigureAwait(false);
        }
        // If this is not an HTTP Trigger function, then we won't attempt to handle the exception and will just re-throw it
        else if (trappedException is not null)
            throw trappedException;
    }

    private async Task HandleHttpTriggerInvocationExceptionAsync(FunctionContext context, Exception trappedException)
    {
        var jsonExceptionHanlder = context.InstanceServices.GetService<JsonMiddlewareExceptionHandler>();

        //If the Json ExceptionHandler was not specified/Configured then we throw the exception
        //  to maintain backwards compatibility with prior behavior.
        if (jsonExceptionHanlder is null)
            throw trappedException;

        var httpResponseData = await GetHttpResponseDataOrDefault(context).ConfigureAwait(false);

        // If we can't create a HttpResponseData then we will just re-throw the original exception because
        //  we have no way to return the error details in a valid response
        if (httpResponseData is null)
            throw trappedException;

        var (httpStatus, result) = jsonExceptionHanlder.Invoke(trappedException);

        //Exceptions do not serilize well, so if the result is an exception we will attempt to convert it to a more
        //  standardized & Json friendly error object with details about the error. If the result is not an exception
        //  then we will just attempt to serialize it as-is; leaving any issues for the Consumer to resolve...
        var sanitizedResult = result is Exception resultException
            ? resultException.ToJsonErrorResultOrDefault()
            : result;

        httpResponseData.StatusCode = httpStatus;
        await httpResponseData.WriteAsJsonAsync(sanitizedResult).ConfigureAwait(false);
        context.GetInvocationResult().Value = httpResponseData;
    }

    private async Task HandleHttpTriggerInvocationAsync(FunctionContext context)
    {
        var invocationResult = context.GetInvocationResult();
        if (invocationResult.Value is not HttpResponseData)
        {
            var httpResponseData = await GetHttpResponseDataOrDefault(context).ConfigureAwait(false);
            if (httpResponseData is not null)
            {
                // Convert the response object to JSON and update the Invocation Result...
                await httpResponseData.WriteAsJsonAsync(invocationResult.Value).ConfigureAwait(false);
                invocationResult.Value = httpResponseData;
            }
        }
    }

    private static async Task<HttpResponseData?> GetHttpResponseDataOrDefault(FunctionContext context)
    {
        HttpResponseData? httpResponseData = await context.GetOrCreateHttpResponseDataAsync().ConfigureAwait(false);
        //NOTE: If HttpRequestData was not injected into the Function there will be no binding so we can't create a response (e.g. it will be null)...
        if (httpResponseData is null)
        {
            context.LogWarning(
                "Unable to create HttpResponseData for Function [{FunctionName}] because no HttpRequestData was injected into the Function. " +
                "This is required to create a valid Http Data Response. The function may or may not return the result in expected Json format but it will likely not have correct Json Content-Type encoding.",
                context.FunctionDefinition.Name
            );
        }

        return httpResponseData;
    }
}