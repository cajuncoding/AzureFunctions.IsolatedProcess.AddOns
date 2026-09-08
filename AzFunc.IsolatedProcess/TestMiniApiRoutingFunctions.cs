using Functions.Worker.AddOns.MiniApiRouting;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace AzFunc.IsolatedProcess;

public sealed class TestMiniApiRoutingFunctions(IMiniApiRouter router)
{
    [Function(nameof(TestMiniApiRoutingFunction))]
    [MiniApiFunction]
    public ValueTask<object?> TestMiniApiRoutingFunction(
        [HttpTrigger(
            AuthorizationLevel.Function,
            MiniApiVerbs.Get,
            MiniApiVerbs.Post,
            Route = "miniapi/widgets/{*path}"
        )]
        HttpRequestData request,
        string? path,
        CancellationToken cancellationToken
    ) => router.DispatchAsync(request, path, cancellationToken);
}
