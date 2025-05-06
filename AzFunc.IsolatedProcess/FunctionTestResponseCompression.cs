using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using AzFunc.IsolatedProcess.Helpers;
using Microsoft.Extensions.Logging;

namespace AzFunc.IsolatedProcess
{
    public class FunctionTestResponseCompression(ILogger iLogger)
    {
        [Function(nameof(FunctionTestResponseCompression))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            var byteSize = (int)ByteSize.FromMegabytes(3.0);
            iLogger.LogInformation($"[{DateTime.Now:O}] Generating Large Text Data Payload of ~[{byteSize:0} MB] . . . ");

            var largePayload = GenerateLargeStringData("!!!DATA", byteSize);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                message = $"Generated Large Payload of [{ByteSize.ToMegabytes(byteSize):0} MB]",
                payload = largePayload
            }).ConfigureAwait(false);
            
            return response;
        }

        private static string GenerateLargeStringData(string repeatText, int targetByteSize)
        {
            var repetitions = (int)Math.Ceiling((double)targetByteSize / repeatText.Length);
            return string
                .Concat(Enumerable.Repeat(repeatText, repetitions))
                .Substring(0, targetByteSize);
        }
    }
}
