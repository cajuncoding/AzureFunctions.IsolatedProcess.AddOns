using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Microsoft.Extensions.Logging;

namespace AzFunc.IsolatedProcess
{
    public class FunctionTestILogger(ILogger iLogger, TestILoggerSingleton singleton, TestILoggerScoped scoped)
    {
        [Function(nameof(FunctionTestILogger))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            var message = $"[{DateTime.Now:O}] C# HTTP trigger function processed a request and Logged using non-generic ILogger 🚀 successfully ✅";
            iLogger.LogInformation(message);
            foreach (var i in Enumerable.Range(0, 10))
                iLogger.LogInformation($"  - [{i:00}]");

            iLogger.LogInformation("Testing Logging from injected TestILoggerSingleton and TestILoggerScoped instances:");
            singleton.LogSomething();
            scoped.LogSomething();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { message }).ConfigureAwait(false);
            return response;
        }
    }

    public abstract class TestILoggerContainer(ILogger logger)
    {
        protected ILogger Logger { get; } = logger;
        public abstract void LogSomething();
    }

    public class TestILoggerSingleton(ILogger logger) : TestILoggerContainer(logger)
    {
        public override void LogSomething() => Logger.LogInformation("Logging from TestILoggerSingleton SINGLETON instance.");
    }

    public class TestILoggerScoped(ILogger logger) : TestILoggerContainer(logger)
    {
        public override void LogSomething() => Logger.LogInformation("Logging from TestILoggerScoped SCOPED instance.");
    }
}
