# Copilot Instructions: Functions.Worker.AddOns.MiniApiRouting

## Project Overview

`Functions.Worker.AddOns.MiniApiRouting` provides compile-time generated Mini APIs for Azure Functions isolated worker.

The project must:

- Preserve Azure Functions isolated-worker hosting.
- Preserve function-level authorization and Function Keys.
- Preserve middleware and dependency injection support.
- Support one or multiple Mini APIs and Functions in the same project.
- Eliminate manual routing switches and lookup tables.
- Eliminate runtime reflection, assembly scanning, and route discovery.
- Generate routing, binding, dispatch, DI registration, and validation at compile time.

The project must not attempt to recreate ASP.NET Core MVC or Minimal APIs. It borrows their lightweight organization while embracing the Azure Functions programming model.

## Target Frameworks

- Runtime library: `net8.0;net10.0`
- Source generator: `netstandard2.0`
- Publish one consumer-facing NuGet package containing the runtime library and generator analyzer.

## Core Programming Model

Every RouteHandler method must be declared inside a class marked with `[MiniApi]`.

The parameterless attribute contributes RouteHandlers to the default Mini API:

```csharp
[MiniApi]
internal sealed class HealthRouteHandlers
{
}
```

A named Mini API uses a constant rather than a repeated string:

```csharp
internal static class MiniApis
{
    internal const string Widgets = @"widgets";
}

[MiniApi(MiniApis.Widgets)]
internal sealed class WidgetRouteHandlers
{
}
```

Multiple classes may contribute RouteHandlers to the same default or named Mini API.

Each hosting Azure Function must explicitly identify its Mini API:

```csharp
[Function(nameof(WidgetFunction))]
[MiniApiFunction(MiniApis.Widgets)]
```

The parameterless `[MiniApiFunction]` hosts the default Mini API.

## RouteHandlers

`[MiniApiRouteHandler]` applies to methods, not classes.

A class may contain one or several RouteHandler methods. RouteHandler classes may also be separated by endpoint when preferred.

RouteHandler methods:

- May have any method name.
- May be static or instance methods.
- Use constructor injection when instance-based.
- Must not be abstract.
- Must not be generic.
- Must be accessible to generated code.
- Do not implement a required interface.

Prefer async examples because service, repository, and external API calls are the primary use cases:

```csharp
[MiniApi(MiniApis.Widgets)]
internal sealed class WidgetRouteHandlers(IWidgetService widgetService)
{
    [MiniApiRouteHandler(MiniApiVerbs.Get, @"{widgetId:int}")]
    public Task<WidgetDto?> GetWidgetAsync(int widgetId, CancellationToken cancellationToken)
        => widgetService.GetWidgetAsync(widgetId, cancellationToken);

    [MiniApiRouteHandler(MiniApiVerbs.Post)]
    public Task<WidgetDto> CreateWidgetAsync(CreateWidgetRequest request, CancellationToken cancellationToken)
        => widgetService.CreateWidgetAsync(request, cancellationToken);
}
```

## HTTP Verb Constants

Provide the following constants through `MiniApiVerbs`:

- `Delete`
- `Get`
- `Head`
- `Options`
- `Patch`
- `Post`
- `Put`

Use these constants in attributes instead of repeated string literals.

## Function Dispatch

`IMiniApiRouter` is registered through dependency injection and injected into the Azure Function class.

The Function-to-Mini-API relationship must remain visible through `[MiniApiFunction]`.

Do not require developers to discover or invoke generated type names directly.

## Parameter Binding

Supported framework parameters:

- `HttpRequestData`
- `FunctionContext`
- `CancellationToken`
- `MiniApiRouteValues`

Convention-based binding precedence:

1. Framework parameter
2. Explicit binding attribute
3. Route value
4. Query-string value
5. Inferred request body
6. Declared C# default value or nullable fallback

### Route Binding

A scalar parameter matching a route token binds from the route:

```csharp
[MiniApiRouteHandler(MiniApiVerbs.Get, @"{widgetId:int}")]
public Task<WidgetDto?> GetWidgetAsync(int widgetId, CancellationToken cancellationToken)
```

Route values take precedence over query-string values with the same name.

`MiniApiRouteValues` must also be available for explicit access to the full route-value collection.

### Query-String Binding

Scalar parameters not matched to route tokens bind from the query string.

Repeated query-string values must support:

- Arrays
- `List<T>`
- `IList<T>`
- `IReadOnlyList<T>`
- `IEnumerable<T>`
- `ICollection<T>`
- `IReadOnlyCollection<T>`

Missing-value behavior:

- Required non-nullable scalar without a default: binding exception
- Nullable scalar: `null`
- Parameter with a C# default: declared default value
- Collection: empty collection

### Header Binding

Headers require explicit binding because header names may contain hyphens or other awkward characters:

```csharp
[MiniApiFromHeader(@"x-correlation-id")]
string? correlationId
```

Do not convention-bind headers by parameter name.

### Request-Body Binding

One complex parameter may bind from the request body automatically:

```csharp
[MiniApiRouteHandler(MiniApiVerbs.Post)]
public Task<WidgetDto> CreateWidgetAsync(CreateWidgetRequest request, CancellationToken cancellationToken)
```

Support optional explicit body binding for edge cases:

```csharp
[MiniApiFromBody]
CreateWidgetRequest request
```

Only one body-bound parameter is allowed per RouteHandler method.

The default implementation must be named:

```csharp
IMiniApiRequestBodyDeserializer
MiniApiJsonRequestBodyDeserializer
```

The default deserializer uses the Azure Functions worker-configured `ObjectSerializer` and supports JSON input. Consumers may replace `IMiniApiRequestBodyDeserializer` through DI to add XML or other formats.

The routing library controls input body deserialization only. It must not control output serialization.

Anything more complex than route, query, explicit header, or single-body binding should remain accessible through `HttpRequestData`.

## Return Types

RouteHandler methods may return:

- `T`
- `Task<T>`
- `ValueTask<T>`
- `void`
- `Task`
- `ValueTask`

Normalize these return shapes only inside generated implementation code. The normalization must not affect the developer-facing RouteHandler API.

Do not serialize or otherwise transform RouteHandler return values. Existing application middleware may handle DTOs, JSON, XML, custom result types, or `HttpResponseData`.

## Route Templates

Support:

- Static segments
- Multiple parameters
- Unconstrained parameters
- Constrained parameters
- Terminal catch-all parameters

Initial constraints:

- `int`
- `long`
- `guid`
- `bool`
- `decimal`

Example:

```text
widgets/{widgetId:int}
assets/{*path}
```

Catch-all parameters must be terminal.

Do not use route-template optional or default syntax such as:

```text
{id?}
{page=1}
```

Optional route segments derive from nullable or defaulted matching method parameters:

```csharp
[MiniApiRouteHandler(MiniApiVerbs.Get, @"{category}/{identifier}")]
public Task<object> GetAsync(string? category = null, string? identifier = null)
```

Optional route parameters must be contiguous and trailing. A required route parameter may not follow an optional route parameter.

The generator must expand effective optional route shapes when checking ambiguity and duplicates.

## Route Precedence

`Priority` defaults to `100`.

Lower numeric priority is evaluated first.

After priority, use natural specificity:

1. Static segments
2. Constrained parameters
3. Unconstrained parameters
4. Catch-all parameters

Use deterministic final ordering when routes otherwise have equal precedence.

## Dependency Injection

Generated registration for instance RouteHandler classes must use `TryAddTransient<T>()`.

This provides a safe default while allowing consumers to override the lifetime through explicit registration.

Static RouteHandler classes require no DI activation.

Register the default request body deserializer and router with `TryAddSingleton` so consumer registrations are respected.

## Runtime Errors

Compile-time-detectable problems must produce generator diagnostics.

Request-specific routing and binding problems must throw descriptive MiniApiRouting exceptions.

Do not generate HTTP error responses. Consumers may use isolated-worker middleware or a DI-injected exception-to-response implementation to map exceptions into JSON, XML, Problem Details, `HttpResponseData`, or another format.

Do not unnecessarily wrap RouteHandler exceptions. Preserve original application exceptions unless additional context is essential and the original exception is retained as `InnerException`.

## Compile-Time Diagnostics

The required baseline diagnostics are:

- `MAR001 MissingMiniApi`
- `MAR002 DuplicateRoute`
- `MAR003 InvalidMethod`
- `MAR004 MultipleBodies`
- `MAR005 InvalidOptionalOrder`
- `MAR006 MissingMiniApiFunction`
- `MAR007 UnboundParameter`

Do not remove a diagnostic without providing equivalent validation.

Adding a `DiagnosticDescriptor` without implementing the corresponding validation and report logic is a bug.

Maintain analyzer release tracking through:

- `AnalyzerReleases.Shipped.md`
- `AnalyzerReleases.Unshipped.md`

Register both files as `AdditionalFiles` in the generator project.

## Source Generator Architecture

Use an incremental source generator.

Organize generator code by responsibility:

```text
Analysis/
Diagnostics/
Discovery/
Extensions/
Generation/
Models/
```

Avoid god classes.

Centralize and reuse:

- Attribute discovery and argument access
- Symbol-name generation
- Route-template parsing
- Effective route-shape generation
- Parameter classification
- Collection classification
- Return-shape analysis
- Diagnostic creation and validation
- Generated string escaping
- Route precedence calculation

Do not duplicate attribute-reading or route-processing logic across generator classes.

Shared implementation should generally use internal helper classes or internal extension methods unless a type has clear public consumer value.

No runtime reflection, assembly scanning, or route-table construction is permitted.

## Coding Style

Prioritize readability, explicit naming, and maintainability.

Prefer:

- `internal` visibility by default
- Small focused classes
- Highly descriptive method names
- Internal extension methods for reusable operations
- Expression-bodied members where they remain readable
- LINQ where it reduces code without repeated enumeration
- Lookups and dictionaries for repeated resolution
- Materializing sequences once when reused
- No braces for simple single-line `if` statements

Avoid:

- Multiple statements on one line
- Nested ternary expressions
- Deeply nested control flow
- God methods or god classes
- Copy-and-paste logic
- Magic strings
- Magic numbers
- Unnecessary allocations or repeated enumeration
- Unnecessary line wrapping

## Wrapped Call Formatting

When a method or constructor call is wrapped, place the closing parenthesis on its own line at the same indentation level as the call start.

Preferred:

```csharp
internal static readonly DiagnosticDescriptor MissingMiniApi = Create(
    @"MAR001",
    @"Missing MiniApi",
    @"RouteHandler method '{0}' must be declared in a class marked with [MiniApi]."
);
```

Do not use:

```csharp
internal static readonly DiagnosticDescriptor MissingMiniApi = Create(
    @"MAR001",
    @"Missing MiniApi",
    @"RouteHandler method '{0}' must be declared in a class marked with [MiniApi].");
```

Apply this convention consistently to:

- Method calls
- Constructor calls
- Diagnostic creation
- Attribute declarations when wrapped
- Multiline LINQ arguments
- Generated-source builder calls

Example:

```csharp
context.ReportDiagnostic(
    Diagnostic.Create(
        MiniApiDiagnostics.DuplicateRoute,
        location,
        verb,
        route,
        group
    )
);
```

## String Handling

Prefer verbatim strings for fixed strings when they simplify syntax:

```csharp
@"MiniApiRouting"
```

Prefer interpolated verbatim strings when interpolation and literal content are both needed:

```csharp
$@"{group}|{verb}|{shape}"
```

Use raw string literals for larger generated source templates when raw literals improve readability. Keep the closing delimiter indentation readable and aligned with the surrounding C# code rather than far-left/outdented in a distracting way. Ideally it should align with the topmost opening logic variable et cetera with the text being invented one level inside the string enclosure treated like braces.

Avoid deeply escaped string literals and fragile chains of string concatenation.

Keep large generated source templates in dedicated internal files or clearly separated template members.

Always inspect generated C# syntax for valid quoting, braces, interpolation, and indentation.

## Documentation and Examples

README documentation must prominently explain:

- Unified function-level key security
- Multiple routes behind one Function when desired
- Multiple Mini APIs and Functions in one project
- Compile-time discovery and validation
- No runtime scanning or reflection
- Route, query, header, and request-body binding
- Flexible return values and middleware integration
- DI replacement of request body deserialization

Use generic personal/open-source examples only:

- Widgets
- Books
- Health checks
- Digital assets
- Forecasts

Do not use business-specific Short URL examples.

Prefer async-first examples throughout documentation.

## Review Requirements

Before considering an implementation complete:

1. Confirm every architectural decision in this file is represented.
2. Confirm every declared diagnostic has reporting logic.
3. Confirm generated code is human-readable.
4. Confirm wrapped calls use the required closing-parenthesis style.
5. Confirm generated strings compile without escaping errors.
6. Confirm the runtime and sample projects target both .NET 8 and .NET 10 where applicable.
7. Confirm analyzer release tracking is configured.
8. Run restore, build, tests, and package validation under an environment containing the required .NET SDKs.
9. Inspect the generated source, not only the generator source.
10. Do not silently remove functionality while refactoring for readability.

## General Guidelines

- Prefer using built-in framework helpers directly when they already express the intent, such as `Enum.IsDefined` for enum validation, instead of custom reimplementation with name enumeration or numeric parsing.
- Factor well-known generated code references such as fully qualified framework members (for example `System.Globalization.NumberStyles.Number` and `CultureInfo.InvariantCulture`) into named constants/helpers instead of embedding them directly in generated expression strings.

## Code Style

- Follow the repository's established code formatting rules.
- Prefer cleaner string processing using string interpolation where appropriate.
