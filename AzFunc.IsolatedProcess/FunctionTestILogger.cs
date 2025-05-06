using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Microsoft.Extensions.Logging;

namespace AzFunc.IsolatedProcess
{
    public class FunctionTestILogger(ILogger iLogger)
    {
        [Function(nameof(FunctionTestILogger))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            var message = $"[{DateTime.Now:O}] C# HTTP trigger function processed a request and Logged using non-generic ILogger 🚀";
            iLogger.LogInformation(message);
            foreach (var i in Enumerable.Range(0, 10))
                iLogger.LogInformation($"  - [{i:00}]");

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { message }).ConfigureAwait(false);
            return response;
        }
    }
}
