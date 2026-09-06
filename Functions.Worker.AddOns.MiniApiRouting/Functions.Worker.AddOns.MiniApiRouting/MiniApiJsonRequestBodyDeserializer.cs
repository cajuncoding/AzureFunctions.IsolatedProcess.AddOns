using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Options;

namespace Functions.Worker.AddOns.MiniApiRouting;

public sealed class MiniApiJsonRequestBodyDeserializer : IMiniApiRequestBodyDeserializer
{
    private const string JsonContentType = "application/json";

    private readonly ObjectSerializer _serializer;

    public MiniApiJsonRequestBodyDeserializer(IOptions<WorkerOptions> options)
    {
        _serializer = options.Value.Serializer ?? new JsonObjectSerializer();
    }

    public async ValueTask<object?> DeserializeAsync(HttpRequestData request, Type targetType, CancellationToken cancellationToken = default)
    {
        if (!request.Headers.TryGetValues("Content-Type", out var contentTypes) || !contentTypes.Any(IsJsonContentType))
            throw new MiniApiUnsupportedContentTypeException(contentTypes?.FirstOrDefault());

        try
        {
            return await _serializer.DeserializeAsync(request.Body, targetType, cancellationToken);
        }
        catch (Exception exception)
        {
            throw new MiniApiParameterBindingException("requestBody", targetType, null, "body", exception);
        }
    }

    private static bool IsJsonContentType(string contentType)
    {
        var mediaType = contentType.Split(';', 2)[0].Trim();
        return mediaType.Equals(JsonContentType, StringComparison.OrdinalIgnoreCase)
            || (mediaType.StartsWith("application/", StringComparison.OrdinalIgnoreCase) && mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase));
    }
}