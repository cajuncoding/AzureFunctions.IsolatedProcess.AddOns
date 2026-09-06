using Microsoft.CodeAnalysis;

namespace Functions.Worker.AddOns.MiniApiRouting.Generators;

internal static class MiniApiDiagnostics
{
    internal static readonly DiagnosticDescriptor MissingMiniApi = Create(
        "MAR001",
        "Missing MiniApi",
        "RouteHandler method '{0}' must be declared in a class marked with [MiniApi]."
    );

    internal static readonly DiagnosticDescriptor DuplicateRoute = Create(
        "MAR002",
        "Duplicate route",
        "Route '{0} {1}' conflicts in Mini API '{2}'."
    );

    internal static readonly DiagnosticDescriptor InvalidMethod = Create(
        "MAR003",
        "Invalid RouteHandler method",
        "RouteHandler method '{0}' cannot be abstract, generic, or inaccessible."
    );

    internal static readonly DiagnosticDescriptor MultipleBodies = Create(
        "MAR004",
        "Multiple body parameters",
        "RouteHandler method '{0}' has more than one body-bound parameter."
    );

    internal static readonly DiagnosticDescriptor InvalidOptionalOrder = Create(
        "MAR005",
        "Invalid optional route order",
        "Required route parameter '{0}' cannot follow an optional route parameter."
    );

    internal static readonly DiagnosticDescriptor MissingMiniApiFunction = Create(
        "MAR006",
        "Unknown Mini API",
        "MiniApiFunction '{0}' references Mini API group '{1}', but that group does not contain any RouteHandlers."
    );

    internal static readonly DiagnosticDescriptor UnboundParameter = Create(
        "MAR007",
        "Unbound parameter",
        "Parameter '{0}' cannot be bound from route, query string, header, request body, or a supported framework value."
    );

    internal static readonly DiagnosticDescriptor UnsupportedRouteConstraint = Create(
        "MAR008",
        "Unsupported route constraint",
        "Route parameter '{0}' uses unsupported constraint '{1}'."
    );

    internal static readonly DiagnosticDescriptor NonTerminalCatchAll = Create(
        "MAR009",
        "Nonterminal catch-all parameter",
        "Catch-all route parameter '{0}' must be the final route segment."
    );

    internal static readonly DiagnosticDescriptor ConflictingBindingAttributes = Create(
        "MAR010",
        "Conflicting binding attributes",
        "Parameter '{0}' declares conflicting MiniApi binding attributes."
    );

    internal static readonly DiagnosticDescriptor RouteTokenWithoutBinding = Create(
        "MAR011",
        "Route token without binding",
        "Route token '{0}' has no compatible method parameter and the method does not accept MiniApiRouteValues."
    );

    internal static readonly DiagnosticDescriptor BodyBindingOnFramework = Create(
        "MAR012",
        "Invalid body binding",
        "Framework parameter '{0}' cannot be bound from the request body."
    );

    internal static readonly DiagnosticDescriptor UnsupportedCollectionElement = Create(
        "MAR013",
        "Unsupported collection element",
        "Parameter '{0}' uses a collection element type that cannot be bound from {1}."
    );

    internal static readonly DiagnosticDescriptor DuplicateMiniApiFunctionAssociation = Create(
        "MAR014",
        "Duplicate MiniApiFunction association",
        "Function method '{0}' declares more than one MiniApiFunction attribute."
    );

    internal static readonly DiagnosticDescriptor InvalidHttpVerb = Create(
        "MAR015",
        "Invalid HTTP verb",
        "RouteHandler method '{0}' declares invalid HTTP verb '{1}'."
    );

    internal static readonly DiagnosticDescriptor InvalidRouteTemplate = Create(
        "MAR016",
        "Invalid route template",
        "Route template '{0}' is invalid: {1}"
    );

    private static DiagnosticDescriptor Create(string id, string title, string message)
        => new(id, title, message, "MiniApiRouting", DiagnosticSeverity.Error, true);
}
