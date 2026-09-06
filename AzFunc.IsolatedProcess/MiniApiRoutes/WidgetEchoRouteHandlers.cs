using Functions.Worker.AddOns.MiniApiRouting;
using Microsoft.Extensions.Logging;
using static AzFunc.IsolatedProcess.MiniApiRoutes.WidgetModels;

namespace AzFunc.IsolatedProcess.MiniApiRoutes;

[MiniApi(MiniApis.WidgetsApi)]
internal sealed class WidgetEchoRouteHandlers(ILogger<WidgetEchoRouteHandlers> logger)
{
    [MiniApiRouteHandler(MiniApiVerbs.Get, "/{widgetId:int}")]
    public async Task<WidgetDto> EchoWidgetIdAsync(
        int widgetId,
        string? material = null,
        [MiniApiFromHeader("x-correlation-id")] string? correlationId = null,
        CancellationToken cancellationToken = default
    )
    {
        // Simulate some async work...✅
        await Task.Delay(200, cancellationToken);

        logger.LogInformation(
            "MiniApi widget id route matched WidgetId={WidgetId}, Material={Material}, CorrelationId={CorrelationId}.",
            widgetId,
            material,
            correlationId
        );

        return new WidgetDto(
            widgetId,
            $"widget-{widgetId}",
            material,
            correlationId,
            "Matched the constrained integer route parameter."
        );
    }

    [MiniApiRouteHandler(MiniApiVerbs.Get, "/{widgetName}")]
    public async Task<WidgetDto> EchoWidgetNameAsync(
        string widgetName,
        string? material = null,
        CancellationToken cancellationToken = default
    )
    {
        // Simulate some async work...✅
        await Task.Delay(200, cancellationToken);

        logger.LogInformation(
            "MiniApi widget name route matched WidgetName={WidgetName}, Material={Material}.",
            widgetName,
            material
        );

        return new WidgetDto(
            null,
            widgetName,
            material,
            CorrelationId: null,
            "Matched the unconstrained string route parameter."
        );
    }

    [MiniApiRouteHandler(MiniApiVerbs.Post, "/{widgetId:int}")]
    public async Task<WidgetDto> EchoWidgetBodyAsync(
        int widgetId,
        WidgetEchoRequest request,
        [MiniApiFromHeader("x-correlation-id")] string? correlationId = null,
        CancellationToken cancellationToken = default
    )
    {
        // Simulate some async work...✅ 
        await Task.Delay(200, cancellationToken);

        logger.LogInformation(
            "MiniApi widget body route matched WidgetId={WidgetId}, Name={Name}, Material={Material}, CorrelationId={CorrelationId}.",
            widgetId,
            request.Name,
            request.Material,
            correlationId
        );

        return new WidgetDto(
            widgetId,
            request.Name,
            request.Material,
            correlationId,
            request.Message ?? "Matched the POST body route."
        );
    }
}

[MiniApi(MiniApis.WidgetsApi)]
internal static class WidgetHealthRouteHandlers
{
    [MiniApiRouteHandler(MiniApiVerbs.Get, "/health", Priority = 0)]
    public static object GetHealth()
        => new // Sync or async methods are fine... ✅
        {
            HealthCheckDateTimeUtc = DateTime.UtcNow,
            Message = "MiniApi widget sample is running. Try /api/miniapi/widgets/1020304 or /api/miniapi/widgets/cog-wheel?material=Titanium."
        };
}
