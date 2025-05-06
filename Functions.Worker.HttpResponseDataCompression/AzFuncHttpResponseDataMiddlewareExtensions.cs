using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Functions.Worker.HttpResponseDataCompression
{
    public static class AzFuncHttpResponseDataMiddlewareExtensions
    {
        public static IFunctionsWorkerApplicationBuilder UseHttpResponseDataCompression(this IFunctionsWorkerApplicationBuilder app, Action<HttpResponseDataCompressionOptions> configAction = null)
            => app.UseMiddleware<HttpResponseDataCompressionMiddleware>();

        public static IServiceCollection ConfigureHttpResponseDataCompression(this IServiceCollection svc, Action<HttpResponseDataCompressionOptions> configAction)
            => svc.Configure(configAction);
    }
}
