using Functions.Worker.AddOns.Common;
using Functions.Worker.AddOns.MiniApiRouting;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using static AzFunc.IsolatedProcess.MiniApiRoutes.WidgetModels;

namespace AzFunc.IsolatedProcess.MiniApiRoutes;

[MiniApi]
internal sealed class WidgetEchoRouteHandlers(ILogger<WidgetEchoRouteHandlers> logger)
{
    [MiniApiGet("/{widgetId:int}")]
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

    [MiniApiGet("/{widgetName}")]
    public async Task<WidgetDto> EchoWidgetNameAsync(
        string widgetName,
        HttpRequestData request,
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
            $"Matched the unconstrained string route parameter. Worked correctly with instance injection of HttpRequestData from Function [{request.FunctionContext.FunctionDefinition.Name}]"
        );
    }


    [MiniApiPost("/bulk")]
    public async Task<IEnumerable<WidgetDto>> EchoBulkWidgetNameRequestAsync(
        IList<WidgetEchoRequest> widgetBulkRequestItems,
        HttpRequestData request,
        string? material = null,
        CancellationToken cancellationToken = default
    )
    {
        // Simulate some async work...✅
        await Task.Delay(200, cancellationToken);

        logger.LogInformation(
            "MiniApi widget name route matched WidgetNamesCsv={WidgetNamesCsv}, Material={Material}.",
            string.Join(", ", widgetBulkRequestItems.Select(i => i.Name)),
            material
        );

        return widgetBulkRequestItems.Select((i, index) => new WidgetDto(
            index,
            i.Name,
            i.Material,
            CorrelationId: Guid.NewGuid().ToString(),
            $"Bulk Collection Read from Body for [{widgetBulkRequestItems.Count}] items."
        ));
    }

    [MiniApiPost("/{widgetId:int}")]
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

[MiniApi]
internal static class WidgetHealthRouteHandlers
{
    [MiniApiGet("/health", Priority = 0)]
    public static object GetHealth(HttpRequestData request, FunctionContext ctx)
        => new // Sync or async methods are fine... ✅
        {
            FunctionName = ctx.FunctionDefinition.Name,
            FunctionAuthKeyName = request.GetFunctionKeyName() ?? "RUNNING_LOCALLY",
            HealthCheckDateTimeUtc = DateTime.UtcNow,
            Message = "MiniApi widget sample is running. Try /api/miniapi/widgets/1020304 or /api/miniapi/widgets/cog-wheel?material=Titanium."
        };
}
