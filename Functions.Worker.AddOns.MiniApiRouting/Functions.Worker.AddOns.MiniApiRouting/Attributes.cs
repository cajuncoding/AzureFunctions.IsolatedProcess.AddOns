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
public class MiniApiRouteHandlerAttribute(string verb, string route = "") : Attribute
{
    public const int DefaultPriority = 100;

    public string Verb { get; } = verb;
    public string Route { get; } = route;
    public int Priority { get; set; } = DefaultPriority;
}

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class MiniApiGetAttribute(string route = "") : MiniApiRouteHandlerAttribute(MiniApiVerbs.Get, route);

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class MiniApiPostAttribute(string route = "") : MiniApiRouteHandlerAttribute(MiniApiVerbs.Post, route);

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class MiniApiPutAttribute(string route = "") : MiniApiRouteHandlerAttribute(MiniApiVerbs.Put, route);

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class MiniApiPatchAttribute(string route = "") : MiniApiRouteHandlerAttribute(MiniApiVerbs.Patch, route);

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class MiniApiDeleteAttribute(string route = "") : MiniApiRouteHandlerAttribute(MiniApiVerbs.Delete, route);

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class MiniApiHeadAttribute(string route = "") : MiniApiRouteHandlerAttribute(MiniApiVerbs.Head, route);

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class MiniApiOptionsAttribute(string route = "") : MiniApiRouteHandlerAttribute(MiniApiVerbs.Options, route);

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class MiniApiFromBodyAttribute : Attribute;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class MiniApiFromHeaderAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}