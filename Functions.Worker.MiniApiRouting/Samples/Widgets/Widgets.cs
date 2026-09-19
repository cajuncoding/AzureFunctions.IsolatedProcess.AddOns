using Functions.Worker.AddOns.MiniApiRouting; using Microsoft.Azure.Functions.Worker; using Microsoft.Azure.Functions.Worker.Http;
namespace Widgets;
internal static class MiniApis { internal const string Widgets="widgets"; }
[MiniApi(MiniApis.Widgets)]
internal sealed class WidgetRouteHandlers(IWidgetService service)
{
    [MiniApiRouteHandler(MiniApiVerbs.Get, "/{widgetId:int}")]
    public Task<WidgetDto?> GetWidgetAsync(int widgetId,CancellationToken cancellationToken)=>service.GetAsync(widgetId,cancellationToken);
    [MiniApiRouteHandler(MiniApiVerbs.Post)]
    public Task<WidgetDto> CreateWidgetAsync(CreateWidgetRequest request,CancellationToken cancellationToken)=>service.CreateAsync(request,cancellationToken);
    [MiniApiRouteHandler(MiniApiVerbs.Get)]
    public Task<IReadOnlyList<WidgetDto>> SearchWidgetsAsync(string? category=null,IReadOnlyList<string>? tag=null,int page=1,CancellationToken cancellationToken=default)=>service.SearchAsync(category,tag??[],page,cancellationToken);
}
internal sealed class WidgetFunction(IMiniApiRouter router)
{
    [Function(nameof(WidgetFunction))][MiniApiFunction(MiniApis.Widgets)]
    public ValueTask<object?> RunAsync([HttpTrigger(AuthorizationLevel.Function,MiniApiVerbs.Get,MiniApiVerbs.Post,Route="widgets/{*path}")] HttpRequestData request,string? path,CancellationToken cancellationToken)=>router.DispatchAsync(request,path,cancellationToken);
}
internal sealed record WidgetDto(int Id,string Name,string Category); internal sealed record CreateWidgetRequest(string Name,string Category);
internal interface IWidgetService { Task<WidgetDto?> GetAsync(int id,CancellationToken ct); Task<WidgetDto> CreateAsync(CreateWidgetRequest request,CancellationToken ct); Task<IReadOnlyList<WidgetDto>> SearchAsync(string? category,IReadOnlyList<string> tags,int page,CancellationToken ct); }
internal sealed class WidgetService:IWidgetService { public Task<WidgetDto?> GetAsync(int id,CancellationToken ct)=>Task.FromResult<WidgetDto?>(new(id,"Flux Capacitor","Experimental")); public Task<WidgetDto>CreateAsync(CreateWidgetRequest r,CancellationToken ct)=>Task.FromResult(new WidgetDto(42,r.Name,r.Category)); public Task<IReadOnlyList<WidgetDto>>SearchAsync(string? c,IReadOnlyList<string> t,int p,CancellationToken ct)=>Task.FromResult<IReadOnlyList<WidgetDto>>([]); }
