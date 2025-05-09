using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;

namespace Functions.Worker.HttpResponseDataJsonMiddleware
{
    public static class AzFuncHttpResponseDataJsonMiddlewareExtensions
    {
        public static IFunctionsWorkerApplicationBuilder UseJsonResponses(this IFunctionsWorkerApplicationBuilder app)
            => app.UseMiddleware<Maestro.BackgroundProcessing.Middleware.HttpResponseDataJsonMiddleware>();
    }
}
