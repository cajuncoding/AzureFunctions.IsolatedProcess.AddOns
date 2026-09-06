namespace Functions.Worker.AddOns.MiniApiRouting;

public sealed class MiniApiRouteNotFoundException : Exception
{
    public MiniApiRouteNotFoundException(string verb, string route)
        : base($"No Mini API route matched {verb} '{route}'.")
    {
        Verb = verb;
        Route = route;
    }

    public string Verb { get; }
    public string Route { get; }
}

public sealed class MiniApiUnsupportedContentTypeException : Exception
{
    public MiniApiUnsupportedContentTypeException(string? contentType)
        : base($"Content type '{contentType ?? "<missing>"}' is not supported by the configured Mini API request body deserializer.")
    {
        ContentType = contentType;
    }

    public string? ContentType { get; }
}

public sealed class MiniApiParameterBindingException : Exception
{
    public MiniApiParameterBindingException(string parameterName, Type targetType, string? value, string source, Exception? innerException = null)
        : base($"Could not bind {source} value '{value ?? "<missing>"}' to parameter '{parameterName}' as {targetType.FullName}.", innerException)
    {
        ParameterName = parameterName;
        TargetType = targetType;
        Value = value;
        BindingSource = source;
    }

    public string ParameterName { get; }
    public Type TargetType { get; }
    public string? Value { get; }
    public string BindingSource { get; }
}
