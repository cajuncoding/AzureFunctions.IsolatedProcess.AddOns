# Functions.Worker.AddOns.MiniApiRouting

Compile-time generated Mini APIs for Azure Functions isolated worker.

## Overview

MiniApiRouting lets one Azure Function host a logical group of HTTP routes while keeping the isolated-worker programming model. Route handlers are ordinary methods discovered by a source generator and dispatched by generated code.

### Give Star 🌟
**If you like this project and/or use it the please give it a Star 🌟 (c'mon it's free, and it'll help others find the project)!**

### [Buy me a Coffee ☕](https://www.buymeacoffee.com/cajuncoding)
*I'm happy to share with the community, but if you find this useful (e.g for professional use), and are so inclinded,
then I do love-me-some-coffee!*

<a href="https://www.buymeacoffee.com/cajuncoding" target="_blank">
<img src="https://cdn.buymeacoffee.com/buttons/default-orange.png" alt="Buy Me A Coffee" height="41" width="174">
</a> 

## Why this package exists

A logical API often needs several routes but one authorization boundary. Creating one Function per route forces consumers to manage several equivalent function keys or move to broader host-level keys. MiniApiRouting allows related routes to share one Function, one authorization level, and one function key.

## Why not one Function per route?

Azure Functions encourages one Function per endpoint.

That works well for many APIs but can become cumbersome when a logical API surface contains numerous related routes that should share the same authorization boundary and Function Key.

MiniApiRouting enables:

- One Function
- One Function Key
- Many Routes
- Compile-time validation
- Compile-time dispatch generation for routing runtime performance
- Ultra lightweight footprint with 'plain vanilla' Azure Functions isolated worker hosting
- No Assembly scanning, reflection, or runtime discovery
- No runtime route-discovery cold-start penalty

... all while preserving the Azure Functions isolated-worker model.

## Goals

- Preserve Azure Functions isolated-worker hosting, middleware, DI, authorization, and keys.
- Support default and named/grouped Mini APIs in the same app (multiple MiniApi Functions can co-exist).
- Generate routing, binding, dispatch, and DI registration at compile time.
- Avoid manual switches, dictionaries, runtime discovery, reflection, or assembly scanning.

## Non-goals

MiniApiRouting is not MVC, controllers, MediatR, pipeline behaviors, output serialization, automatic HTTP error responses, form-data binding, cookie binding, or arbitrary body-format binding.

## Compile-time advantages

The generator validates route handlers, ambiguous routes, optional path rules, binding conflicts, body parameters, route templates, and Function-to-Mini-API associations before runtime.

## Supported frameworks

- Runtime: `net8.0`, `net10.0`
- Source generator: `netstandard2.0`

## Installation

```powershell
dotnet add package Functions.Worker.AddOns.MiniApiRouting
```

The package includes the runtime library and generator analyzer. Consumers install only this package.

## Registration

```csharp
builder.Services.AddFunctionsMiniApiRouting();
```

`AddFunctionsMiniApiRouting()` registers the default `IMiniApiRequestBodyDeserializer` and applies the source-generated registrations for RouteHandler classes and `IMiniApiRouter`.
Generated registrations use `TryAddTransient` and `TryAddSingleton`, allowing applications to provide explicit registrations when needed.

## Async-first Widget API example

```csharp
internal static class MiniApis
{
    internal const string Widgets = "widgets";
}

[MiniApi]
internal sealed class WidgetRouteHandlers(IWidgetService widgetService)
{
    [MiniApiGet("/{{widgetId:int}}")]
    public Task<WidgetDto?> GetWidgetAsync(int widgetId, string? include = null, CancellationToken cancellationToken = default)
        => widgetService.GetWidgetAsync(widgetId, include, cancellationToken);

    [MiniApiPost]
    public Task<WidgetDto> CreateWidgetAsync(CreateWidgetRequest request, CancellationToken cancellationToken)
        => widgetService.CreateWidgetAsync(request, cancellationToken);
}
```

## Default Mini API example

Use `[MiniApi]` and `[MiniApiFunction]` without a name for the default group.

## Named Mini API example

Use API grouping names (constants make this clean) with `[MiniApi(MiniApis.Widgets)]` and `[MiniApiFunction(MiniApis.Widgets)]`. Group names are never inferred from class names.

## Multiple Mini APIs and Functions example

Several classes can contribute handlers to the same group, and several Functions can host different groups in one app.

## Function delegation

```csharp
internal sealed class WidgetFunction(IMiniApiRouter router)
{
    [Function(nameof(WidgetFunction))]
    [MiniApiFunction]
    public ValueTask<object?> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, MiniApiVerbs.Get, MiniApiVerbs.Post, Route = "widgets/{*path}")] HttpRequestData request,
        string? path,
        CancellationToken cancellationToken
    ) => router.DispatchAsync(request, path, cancellationToken);
}
```

The generated router resolves the Mini API group from the current Function name and `MiniApiFunctionAttribute`.

## Route parameter binding

Scalar parameters matching route tokens bind from route values and take precedence over query strings.

## Query-string and repeated collection binding

Scalar query parameters bind from `HttpRequestData.Query`. Repeated values support arrays, `List<T>`, `IList<T>`, `IReadOnlyList<T>`, `IEnumerable<T>`, `ICollection<T>`, and `IReadOnlyCollection<T>` for supported scalar element types. Scalar parameters use the first value deterministically.

## Explicit header binding

Headers require `[MiniApiFromHeader("x-correlation-id")]` and are not bound by convention.

## Inferred and explicit body binding

One complex parameter binds from the JSON body automatically. Use `[MiniApiFromBody]` to opt in explicitly.

## Custom IMiniApiRequestBodyDeserializer example

Register a custom `IMiniApiRequestBodyDeserializer` before `AddFunctionsMiniApiRouting` to support XML or another format. The default package ships JSON support only and uses the Azure Functions worker-configured `ObjectSerializer`.

## Route priority and natural specificity

`MiniApiRouteHandlerAttribute.Priority` defaults to `100`. Lower values run first. Ties prefer static segments, constrained parameters, unconstrained parameters, then catch-all parameters.

## Catch-all route example

```csharp
[MiniApiGet("/assets/{*path}")]
public Task<DigitalAsset?> GetAssetAsync(string path, CancellationToken cancellationToken)
    => assets.GetAsync(path, cancellationToken);
```

Catch-all parameters must be terminal.

## Optional trailing path parameters

Nullable or defaulted trailing route parameters are optional. Optional route parameters must be contiguous and trailing.

## Supported return shapes

Handlers may return `T`, `Task<T>`, `ValueTask<T>`, `void`, `Task`, or `ValueTask`. Results are not serialized or converted by MiniApiRouting.

## Exception handling

Routing and binding failures throw `MiniApiRouteNotFoundException`, `MiniApiParameterBindingException`, or `MiniApiUnsupportedContentTypeException`. The library does not create HTTP error responses.

## Compile-time diagnostic table

| ID | Description |
| --- | --- |
| MAR001 | Missing `[MiniApi]` on a RouteHandler class |
| MAR002 | Duplicate or ambiguous route |
| MAR003 | Invalid RouteHandler method |
| MAR004 | Multiple body-bound parameters |
| MAR005 | Invalid optional route parameter order |
| MAR006 | Function references a group with no handlers |
| MAR007 | Unbound parameter |
| MAR008 | Unsupported route constraint |
| MAR009 | Nonterminal catch-all |
| MAR010 | Conflicting binding attributes |
| MAR011 | Route token without binding destination |
| MAR012 | Body binding on framework parameter |
| MAR013 | Unsupported query/header collection element |
| MAR014 | Duplicate MiniApiFunction association if the attribute model permits it |
| MAR015 | Invalid HTTP verb |
| MAR016 | Invalid route template syntax |

## Package architecture

The NuGet package contains runtime assemblies under `lib/net8.0` and `lib/net10.0` plus the generator under `analyzers/dotnet/cs`.

## Contributing and validation commands

```powershell
dotnet restore
dotnet build -c Release
dotnet test -c Release --no-build
dotnet pack -c Release --no-build
```

## Current release status

Initial implementation released under active validation in production use cases.
