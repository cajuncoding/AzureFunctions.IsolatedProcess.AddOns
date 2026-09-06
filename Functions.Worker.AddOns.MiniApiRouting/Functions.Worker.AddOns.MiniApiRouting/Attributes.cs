namespace Functions.Worker.AddOns.MiniApiRouting;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MiniApiAttribute(string? name = null) : Attribute
{
    public string? Name { get; } = name;
}

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class MiniApiFunctionAttribute(string? name = null) : Attribute
{
    public string? Name { get; } = name;
}

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class MiniApiRouteHandlerAttribute(string verb, string route = "") : Attribute
{
    public const int DefaultPriority = 100;

    public string Verb { get; } = verb;
    public string Route { get; } = route;
    public int Priority { get; set; } = DefaultPriority;
}

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class MiniApiFromBodyAttribute : Attribute;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class MiniApiFromHeaderAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
