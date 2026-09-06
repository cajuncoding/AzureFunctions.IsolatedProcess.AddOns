namespace AzFunc.IsolatedProcess.MiniApiRoutes;


internal static class MiniApis
{
    internal const string WidgetsApi = "widgets";
}

public static class WidgetModels
{
    public sealed record WidgetEchoRequest(
        string Name,
        string? Material = null,
        string? Message = null
    );

    public sealed record WidgetDto(
        int? Id,
        string Name,
        string? Material,
        string? CorrelationId,
        string Message
    );
}
