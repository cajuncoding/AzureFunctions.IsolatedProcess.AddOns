using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Functions.Worker.AddOns.Common
{
    public static class FunctionContextCustomExtensions
    {
        public const string HttpTriggerBindingTypeName = "httpTrigger";

        public static bool IsHttpTriggerFunction(this FunctionContext? functionContext)
            => functionContext?.FunctionDefinition.InputBindings.Values.Any(
                binding => binding.Type.Equals(HttpTriggerBindingTypeName, StringComparison.OrdinalIgnoreCase)
            ) ?? false;

        public static async ValueTask<HttpResponseData?> GetOrCreateHttpResponseDataAsync(this FunctionContext? functionContext)
        {
            if (functionContext is null) return null;
            return functionContext.GetHttpResponseData() ?? (await functionContext.GetHttpRequestDataAsync().ConfigureAwait(false))?.CreateResponse();
        }

        public static ILogger? GetLogger(this FunctionContext? functionContext)
            => functionContext?.GetLogger(functionContext.FunctionDefinition.Name);

        public static FunctionContext LogTrace(this FunctionContext functionContext, string message, params object?[] args)
        {
            functionContext.GetLogger()?.LogTrace(message, args);
            return functionContext;
        }
        public static FunctionContext LogInformation(this FunctionContext functionContext, string message, params object?[] args)
        {
            functionContext.GetLogger()?.LogInformation(message, args);
            return functionContext;
        }

        public static FunctionContext LogWarning(this FunctionContext functionContext, string message, params object?[] args)
        {
            functionContext.GetLogger()?.LogWarning(message, args);
            return functionContext;
        }

        public static FunctionContext LogError(this FunctionContext functionContext, Exception exc, string? message = null, params object?[] args)
        {
            functionContext.GetLogger()?.LogError(exc, message, args);
            return functionContext;
        }
    }
}