using Microsoft.Azure.Functions.Worker.Http;

namespace Functions.Worker.AddOns.MiniApiRouting;

public interface IMiniApiRouter
{
    ValueTask<object?> DispatchAsync(
        HttpRequestData request,
        string? relativePath = null,
        CancellationToken cancellationToken = default
    );
}

public interface IMiniApiRequestBodyDeserializer
{
    ValueTask<object?> DeserializeAsync(
        HttpRequestData request,
        Type targetType,
        CancellationToken cancellationToken = default
    );
}
