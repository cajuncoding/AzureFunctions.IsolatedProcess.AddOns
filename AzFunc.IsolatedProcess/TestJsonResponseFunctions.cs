using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace AzFunc.IsolatedProcess
{
    public class TestJsonResponseFunctions()
    {
        private static int _callCounter = 0;

        [Function(nameof(TestAnonymousPocoJsonFunction))]
        public Task<object> TestAnonymousPocoJsonFunction([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
            => CreateAnonymousDtoResultAsync();

        [Function(nameof(TestMissingHttpRequestDataFunction))]
        public Task<object> TestMissingHttpRequestDataFunction([HttpTrigger(AuthorizationLevel.Function, "get", "post")] ILoggerFactory logFactoryMissingHttpRequestData)
            => CreateAnonymousDtoResultAsync("Should Show a Warning about the missing HttpRequestData and format will be text/plain since HttpResponseData was not used to generate a valid Json result!");

        [Function(nameof(TestExceptionThrownJsonHandlingBadRequestFunction))]
        public Task<object> TestExceptionThrownJsonHandlingBadRequestFunction([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
            => throw new InvalidOperationException($"Should Show an Exception message in the logs and response as [{HttpStatusCode.Conflict.ToString()}] HTTP Status for the {nameof(InvalidOperationException)} being thrown!");

        [Function(nameof(TestExceptionThrownJsonHandlingUnauthorizedFunction))]
        public Task<object> TestExceptionThrownJsonHandlingUnauthorizedFunction([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
            => throw new UnauthorizedAccessException($"Should Show an Exception message in the logs and response as a [{HttpStatusCode.Unauthorized.ToString()}] HTTP Status for the {nameof(UnauthorizedAccessException)} being thrown!");


        private Task<object> CreateAnonymousDtoResultAsync(string? message = null) => Task.FromResult<object>(new
        {
            TestName = nameof(TestAnonymousPocoJsonFunction),
            DateTimeNow = DateTime.Now.ToString("O"),
            Message = message ?? "C# HTTP trigger function processed a request with an Anonymous C# POCO/DTO automatically handled as Json 🚀",
            CallCount = Interlocked.Increment(ref _callCounter),
        });
    }
}