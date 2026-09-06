using AzFunc.IsolatedProcess.MiniApiRoutes;
using Functions.Worker.AddOns.MiniApiRouting;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AzFunc.IsolatedProcess;

public sealed class TestMiniApiRoutingFunctions(IMiniApiRouter router)
{
    [Function(nameof(TestMiniApiRoutingFunction))]
    [MiniApiFunction(MiniApis.WidgetsApi)]
    public ValueTask<object?> TestMiniApiRoutingFunction(
        [HttpTrigger(
            AuthorizationLevel.Function,
            MiniApiVerbs.Get,
            MiniApiVerbs.Post,
            Route = "miniapi/widgets/{*path}"
        )]
        HttpRequestData request,
        string? path,
        FunctionContext ctx,
        CancellationToken cancellationToken
    )
    {
        ctx.InstanceServices.GetService<IMiniApiRouter>();
        return router.DispatchAsync(request, path, cancellationToken);
    }
}
