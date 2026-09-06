using Microsoft.CodeAnalysis;

namespace Functions.Worker.AddOns.MiniApiRouting.Generators;

internal sealed record ParameterModel(
    string Name,
    string TypeName,
    string? ElementTypeName,
    BindingSource Source,
    bool Optional,
    bool IsNullable,
    string? HeaderName,
    string? DefaultValue,
    CollectionKind Collection,
    FrameworkParameterKind FrameworkKind,
    bool HasBindingConflict,
    bool IsFrameworkBody,
    bool HasUnsupportedCollectionElement,
    bool IsUnbound
);

internal sealed record RouteModel(
    string Group,
    string Verb,
    string Template,
    int Priority,
    string ContainingType,
    string MethodName,
    bool IsStatic,
    bool IsInvalidMethod,
    ReturnKind ReturnKind,
    IReadOnlyList<ParameterModel> Parameters,
    IReadOnlyList<RouteSegment> Segments,
    IReadOnlyList<RouteTemplateDiagnostic> TemplateDiagnostics,
    Location? Location
);

internal sealed record FunctionModel(string Group, string FunctionName, bool IsDuplicateAssociation, Location? Location);

internal sealed record RouteTemplateDiagnostic(RouteTemplateDiagnosticKind Kind, string Value, string? Detail);

internal enum BindingSource { Framework, Route, Query, Header, Body, Unbound }
internal enum CollectionKind { None, Array, List, Interface }
internal enum FrameworkParameterKind { None, Request, Context, CancellationToken, RouteValues }
internal enum ReturnKind { Void, Value, Task, TaskValue, ValueTask, ValueTaskValue }
internal enum RouteSegmentKind { Literal, Parameter, CatchAll }
internal enum RouteTemplateDiagnosticKind { InvalidSyntax, UnsupportedConstraint, NonTerminalCatchAll }
