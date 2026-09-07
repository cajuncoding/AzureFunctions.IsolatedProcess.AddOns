# Functions.Worker.AddOns.MiniApiRouting

Compile-time generated Mini APIs for Azure Functions isolated worker. 

When you want many API routes for one Function, and single/consolidated Function Key management, you want an Azure Funtion MiniApi!

## Overview

MiniApiRouting lets one Azure Function host a logical group of HTTP routes while preserving the isolated-worker programming model. RouteHandlers are ordinary methods discovered by a source generator and dispatched through focused generated code.

```text
One Function
One Function Key
One Authorization Boundary
Many Routes
```

MiniApiRouting provides compile-time route validation and routing without assembly scanning, reflection, or runtime route discovery.

### Give the project a Star 🌟

**If you like this project or use it, please give it a Star. It is free and helps others find the project!**

### Buy me a Coffee ☕

I am happy to share with the community, but if you find this useful, especially for professional use, then I do love me some coffee!

<a href="https://www.buymeacoffee.com/cajuncoding" target="_blank" rel="noopener noreferrer"><img src="https://cdn.buymeacoffee.com/buttons/default-orange.png" alt="Buy Me A Coffee" height="41" width="174"></a>

## Why not one Function per route?

Azure Functions encourages one Function per endpoint. That works well for many APIs, but it can become cumbersome when a logical API contains several related routes that should share the same authorization boundary and Function Key.

MiniApiRouting enables:

- One Function
- One Function Key
- Many routes
- Compile-time validation
- Compile-time dispatch generation for routing runtime performance
- Ultra lightweight footprint with plain vanilla Azure Functions isolated-worker hosting
- No assembly scanning, reflection, or runtime discovery
- No runtime route-discovery cold-start penalty

All while preserving Azure Functions isolated-worker hosting, middleware, dependency injection, authorization, and keys.

## Installation

```powershell
dotnet add package Functions.Worker.AddOns.MiniApiRouting
```

The package includes the runtime library and source-generator analyzer. Consumers install only this package.

Supported frameworks:

- Runtime: `net8.0`, `net10.0`
- Source generator: `netstandard2.0`

## Quick start

### 1. Register MiniApiRouting

```csharp
builder.Services.AddFunctionsMiniApiRouting();
```

`AddFunctionsMiniApiRouting()` registers the default `IMiniApiRequestBodyDeserializer` and applies source-generated registrations for RouteHandler classes and `IMiniApiRouter`.

Generated registrations use `TryAddTransient` and `TryAddSingleton`, allowing applications to provide explicit registration to override with custom behavior when needed.

### 2. Define RouteHandlers

```csharp
[MiniApi]
internal sealed class WidgetRouteHandlers(IWidgetService widgets)
{
    [MiniApiGet]
    public Task<IReadOnlyList<WidgetDto>> GetWidgetsAsync(CancellationToken cancellationToken = default)
        => widgets.GetWidgetsAsync(cancellationToken);

    [MiniApiGet("/{widgetId:int}")]
    public Task<WidgetDto?> GetWidgetAsync(int widgetId, string? material = null, CancellationToken cancellationToken = default)
        => widgets.GetWidgetAsync(widgetId, material, cancellationToken);

    [MiniApiPost]
    public Task<WidgetDto> CreateWidgetAsync(CreateWidgetRequest createPayload, CancellationToken cancellationToken = default)
        => widgets.CreateWidgetAsync(createPayload, cancellationToken);

    [MiniApiPut("/{widgetId:int}")]
    public Task<WidgetDto> UpdateWidgetAsync(int widgetId, UpdateWidgetRequest updatePayload, CancellationToken cancellationToken = default)
        => widgets.UpdateWidgetAsync(widgetId, updatePayload, cancellationToken);

    [MiniApiDelete("/{widgetId:int}")]
    public Task DeleteWidgetAsync(int widgetId, CancellationToken cancellationToken = default)
        => widgets.DeleteWidgetAsync(widgetId, cancellationToken);
}
```

The convenience attributes cover the supported HTTP verbs:

```csharp
[MiniApiGet]
[MiniApiPost]
[MiniApiPut]
[MiniApiPatch]
[MiniApiDelete]
[MiniApiHead]
[MiniApiOptions]
```

The lower-level form remains available when needed:

```csharp
[MiniApiRouteHandler(MiniApiVerbs.Get, "/{widgetId:int}")]
```

### 3. Create the hosting Function

```csharp
internal sealed class WidgetApiFunction(IMiniApiRouter router)
{
    [Function(nameof(WidgetApiFunction))]
    [MiniApiFunction]
    public ValueTask<object?> RunAsync(
        [HttpTrigger(
            AuthorizationLevel.Function,
            //Only define the verbs needed for the API…
            MiniApiVerbs.Get,
            MiniApiVerbs.Post,
            MiniApiVerbs.Put,
            MiniApiVerbs.Patch,
            MiniApiVerbs.Delete
            Route = "widgets/{*path}"
        )]
        HttpRequestData request,
        string? path,
        CancellationToken cancellationToken
    ) => router.DispatchAsync(request, path, cancellationToken);
}
```

That is the complete routing setup.

## Route patterns

### Root routes

```csharp
[MiniApiGet]
public Task<IReadOnlyList<WidgetDto>> GetWidgetsAsync()
```

Matches:

```text
GET /api/widgets
```

### Route parameters

```csharp
[MiniApiGet("/{widgetId:int}")]
public Task<WidgetDto?> GetWidgetAsync(int widgetId)
```

Matches:

```text
GET /api/widgets/42
```

The route token name must match its RouteHandler parameter. Route values are properly URL-decoded before binding, including encoded spaces and Unicode characters.

### Multiple route parameters

```csharp
[MiniApiGet("/{category}/{widgetId:int}")]
public Task<WidgetDto?> GetWidgetAsync(string category, int widgetId)
```

Matches:

```text
GET /api/widgets/tools/42
```

### Optional trailing route parameters

Route parameters become optional when the matching C# parameter is nullable or has a default value.

As optional then when not bound from the request the default value will be bound id defined, or the binding will be null if no default is defined.

```csharp
[MiniApiGet("/{category}/{name}")]
public Task<IReadOnlyList<WidgetDto>> SearchWidgetsAsync(string? category = null, string? name = null)
```

Matches:

```text
GET /api/widgets
GET /api/widgets/tools
GET /api/widgets/tools/hammer
```

Optional route parameters must be contiguous and trailing. A required route segment cannot follow an optional route parameter.

### Catch-all routes

```csharp
[MiniApiGet("/assets/{*path}")]
public Task<DigitalAsset?> GetAssetAsync(string path, CancellationToken cancellationToken = default)
```

Matches:

```text
GET /api/widgets/assets
GET /api/widgets/assets/logo.png
GET /api/widgets/assets/images/icons/logo.png
```

The bound `path` values are:

```text
""
"logo.png"
"images/icons/logo.png"
```

Catch-all parameters must be terminal; the last match segment of the route. Encoded route values are decoded after segment matching, so an encoded slash remains within the captured segment during matching and is decoded before binding.

## Route constraints

Constraints validate route shape before a RouteHandler is selected.

| Constraint | Example | Matches |
| --- | --- | --- |
| `string` | `{name:string}` | Any string value |
| `int` | `{id:int}` | 32-bit integer |
| `long` | `{id:long}` | 64-bit integer |
| `guid` | `{id:guid}` | GUID value |
| `bool` | `{enabled:bool}` | `true` or `false` |
| `decimal` | `{price:decimal}` | Invariant-culture decimal (rational) number |

Examples:

```csharp
[MiniApiGet("/{widgetId:int}")]
public Task<WidgetDto?> GetWidgetByIdAsync(int widgetId)
```

```csharp
[MiniApiGet("/{assetIdentifier:guid}")]
public Task<DigitalAsset?> GetAssetByIdentifierAsync(Guid assetIdentifier)
```

```csharp
[MiniApiGet("/{enabled:bool}")]
public Task<IReadOnlyList<WidgetDto>> GetWidgetsAsync(bool enabled)
```

```csharp
[MiniApiGet("/{price:decimal}")]
public Task<IReadOnlyList<WidgetDto>> GetWidgetsAsync(decimal price)
```
For rational number input constraint `decimal` provides the general numeric route shape constraint but may be used when the matching parameter binds to any supported rational numeric type such as `decimal`, `double`, `float`, etc.

## Parameter binding

MiniApiRouting binds RouteHandler parameters in this order:

1. Supported framework parameters
2. Explicit binding attributes
3. Route values
4. Query-string values
5. One inferred request body
6. Declared C# default values or nullable fallbacks

Route values take precedence over query-string values with the same name.

### Framework parameters

RouteHandlers may receive:

```csharp
HttpRequestData
FunctionContext
CancellationToken
MiniApiRouteValues
```

### Query-string values

Scalar parameters not matched to route tokens bind from `HttpRequestData.Query`.

```csharp
[MiniApiGet("/{widgetId:int}")]
public Task<WidgetDto?> GetWidgetAsync(int widgetId, string? material = null)
```

Request:

```text
GET /api/widgets/42?material=titanium
```

Repeated query values support arrays, `List<T>`, `IList<T>`, `IReadOnlyList<T>`, `IEnumerable<T>`, `ICollection<T>`, and `IReadOnlyCollection<T>` for supported scalar element types.

```csharp
[MiniApiGet]
public Task<IReadOnlyList<WidgetDto>> SearchWidgetsAsync(string[] tags)
```

Request:

```text
GET /api/widgets?tags=tools&tags=featured
```

Scalar parameters use the first value deterministically. Missing collections bind as empty collections.

### Header values

Headers require explicit binding and are not bound by convention.

```csharp
[MiniApiGet("/{widgetId:int}")]
public Task<WidgetDto?> GetWidgetAsync(
    int widgetId,
    [MiniApiFromHeader("x-correlation-id")] string? correlationId = null
)
```

### Request body

One single complex parameter binds from the JSON request body automatically.

```csharp
[MiniApiPost]
public Task<WidgetDto> CreateWidgetAsync(CreateWidgetRequest request)
```

Use `[MiniApiFromBody]` when explicit body binding is preferred.

The default `MiniApiJsonRequestBodyDeserializer` uses the Azure Functions worker-configured `ObjectSerializer` and accepts `application/json` and structured `application/*+json` content types.

Applications can support XML or another format by registering a custom `IMiniApiRequestBodyDeserializer` before calling `AddFunctionsMiniApiRouting()`.

## Default and named Mini APIs

Most applications can use the default group:

```csharp
[MiniApi]
internal sealed class WidgetRouteHandlers
{
}

[MiniApiFunction]
```

When one Function app hosts several independent APIs, use named groups to separate the sets of APIs and dispatch them from separate root Functions.

Using Constants can help keep the association clean and explicit.

```csharp
internal static class MiniApis
{
    internal const string Widgets = "widgets";
    internal const string Assets = "assets";
}

[MiniApi(MiniApis.Widgets)]
internal sealed class WidgetRouteHandlers
{
}

[MiniApiFunction(MiniApis.Widgets)]
```

Multiple classes may contribute RouteHandlers to the same default or named group. Group names are never inferred from class names. This allows implementations to decide how they want to structure and organize thier code.

## Route priority and specificity

`MiniApiRouteHandlerAttribute.Priority` defaults to `100`. Lower numbers are evaluated first.

When priorities are equal, routes are ordered naturally:

1. Static segments
2. Constrained parameters
3. Unconstrained parameters
4. Catch-all parameters

Use an explicit priority only when natural route specificity does not express the desired behavior.

```csharp
[MiniApiGet("/health", Priority = 0)]
public static object GetHealth()
    => new { Status = "Healthy" };
```

## Return values

RouteHandlers may return:

```text
T
Task<T>
ValueTask<T>
void
Task
ValueTask
```

MiniApiRouting does not serialize or transform RouteHandler results. RouteHandlers may return DTOs, custom results, `HttpResponseData`, or any application-specific value. Existing Function code or middleware remains responsible for output handling.

## Compile-time validation

MiniApiRouting reports actionable compiler errors for invalid RouteHandler declarations, malformed or ambiguous routes, unsupported constraints, optional-route ordering, catch-all placement, binding conflicts, multiple request bodies, missing route bindings, and invalid Function-to-Mini-API associations.

These issues are found during compilation rather than after deployment or during request processing.

## Runtime errors

Request-specific routing and binding failures use:

- `MiniApiRouteNotFoundException`
- `MiniApiParameterBindingException`
- `MiniApiUnsupportedContentTypeException`

The library does not create HTTP error responses. Applications may use isolated-worker middleware to map exceptions to JSON, XML, Problem Details, `HttpResponseData`, or another response format.

## Non-goals (What MiniApi is not trying to do!)

MiniApiRouting is not trying to replace or be AspNetCore MVC, controllers, MediatR, pipeline behaviors, output serialization, automatic HTTP error responses, form-data binding, cookie binding, or arbitrary body-format binding.

## Current release status

Initial implementation released under active validation in production use cases.
