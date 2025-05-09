using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AzFunc.IsolatedProcess
{
    public class FunctionTestJsonResponse(ILogger iLogger)
    {
        private static int _callCounter = 0;

        [Function(nameof(TestAnonymousPocoJsonFunction))]
        public Task<object> TestAnonymousPocoJsonFunction([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
            => CreateAnonymousDtoResultAsync();

        [Function(nameof(TestMissingHttpRequestDataFunction))]
        public Task<object> TestMissingHttpRequestDataFunction([HttpTrigger(AuthorizationLevel.Function, "get", "post")] ILoggerFactory logFactoryMissingHttpRequestData)
            => CreateAnonymousDtoResultAsync("Should Show a Warning about the missing HttpRequestData");

        private Task<object> CreateAnonymousDtoResultAsync(string? message = null) => Task.FromResult<object>(new
        {
            TestName = nameof(TestAnonymousPocoJsonFunction),
            DateTimeNow = DateTime.Now.ToString("O"),
            Message = message ?? "C# HTTP trigger function processed a request with an Anonymous C# POCO/DTO automatically handled as Json 🚀",
            CallCount = Interlocked.Increment(ref _callCounter),
        });
    }
}