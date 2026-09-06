using Microsoft.Azure.Functions.Worker.Http;

namespace Functions.Worker.AddOns.MiniApiRouting;

public static class HttpRequestDataExtensions
{
    public static string[] GetQueryValues(this HttpRequestData request, string name)
        => request.Query.GetValues(name) ?? Array.Empty<string>();
    
    public static string[] GetHeaderValues(this HttpRequestData request, string name)
        => request.Headers.TryGetValues(name, out var values) ? values.ToArray() : Array.Empty<string>();
}
