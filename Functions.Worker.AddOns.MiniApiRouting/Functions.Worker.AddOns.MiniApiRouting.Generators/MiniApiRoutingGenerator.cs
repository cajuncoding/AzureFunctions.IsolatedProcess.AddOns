using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Functions.Worker.AddOns.MiniApiRouting.Generators;

[Generator]
public sealed class MiniApiRoutingGenerator : IIncrementalGenerator
{
    private static readonly HashSet<string> ValidVerbs = new(StringComparer.OrdinalIgnoreCase)
    {
        "DELETE",
        "GET",
        "HEAD",
        "OPTIONS",
        "PATCH",
        "POST",
        "PUT"
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var routes = context.SyntaxProvider.ForAttributeWithMetadataName(WellKnownMetadataNames.MiniApiRouteHandlerAttribute, IsMethod, CreateRouteModel).Collect();
        var functions = context.SyntaxProvider.ForAttributeWithMetadataName(WellKnownMetadataNames.MiniApiFunctionAttribute, IsMethod, CreateFunctionModel).Collect();
        context.RegisterSourceOutput(routes.Combine(functions), Generate);
    }

    private static bool IsMethod(SyntaxNode node, CancellationToken _) => node is MethodDeclarationSyntax;

    private static RouteModel CreateRouteModel(GeneratorAttributeSyntaxContext context, CancellationToken _)
    {
        var method = (IMethodSymbol)context.TargetSymbol;
        var routeAttribute = context.Attributes[0];
        var miniApiAttribute = method.ContainingType.GetAttribute(WellKnownMetadataNames.MiniApiAttribute);
        var route = RouteTemplate.Normalize(routeAttribute.GetConstructorString(1) ?? string.Empty);
        var parameters = MethodAnalyzer.AnalyzeParameters(method, route);
        var parameterLookup = parameters.ToDictionary(parameter => parameter.Name, StringComparer.OrdinalIgnoreCase);
        var segments = RouteTemplate.Parse(route, parameterLookup, out var templateDiagnostics);

        return new(
            miniApiAttribute?.GetConstructorString() ?? (miniApiAttribute is null ? "#missing" : string.Empty),
            (routeAttribute.GetConstructorString() ?? string.Empty).ToUpperInvariant(),
            route,
            routeAttribute.GetNamedInt("Priority", 100),
            method.ContainingType.GetFullyQualifiedName(),
            method.Name,
            method.IsStatic,
            MethodAnalyzer.IsInvalidRouteHandlerMethod(method),
            MethodAnalyzer.AnalyzeReturnKind(method),
            parameters,
            segments,
            templateDiagnostics,
            method.Locations.FirstOrDefault()
        );
    }

    private static FunctionModel CreateFunctionModel(GeneratorAttributeSyntaxContext context, CancellationToken _)
    {
        var method = (IMethodSymbol)context.TargetSymbol;
        var functionAttribute = method.GetAttribute(WellKnownMetadataNames.FunctionAttribute);

        return new(
            context.Attributes[0].GetConstructorString() ?? string.Empty,
            functionAttribute?.GetConstructorString() ?? method.Name,
            context.Attributes.Length > 1,
            method.Locations.FirstOrDefault()
        );
    }

    private static void Generate(SourceProductionContext context, (ImmutableArray<RouteModel> Left, ImmutableArray<FunctionModel> Right) models)
    {
        var routes = models.Left;
        var functions = models.Right;
        ReportDiagnostics(context, routes, functions);

        var validRoutes = routes.Where(IsValidForEmission).ToArray();
        if (validRoutes.Length > 0)
            context.AddSource("MiniApiGenerated.g.cs", MiniApiEmitter.Emit(validRoutes, functions));
    }

    private static void ReportDiagnostics(SourceProductionContext context, IEnumerable<RouteModel> routes, IEnumerable<FunctionModel> functions)
    {
        var materializedRoutes = routes.ToArray();
        var materializedFunctions = functions.ToArray();

        foreach (var route in routes.Where(route => route.Group == "#missing"))
            context.ReportDiagnostic(Diagnostic.Create(MiniApiDiagnostics.MissingMiniApi, route.Location, route.MethodName));

        foreach (var route in materializedRoutes.Where(route => route.IsInvalidMethod))
            context.ReportDiagnostic(Diagnostic.Create(MiniApiDiagnostics.InvalidMethod, route.Location, route.MethodName));

        foreach (var route in materializedRoutes.Where(route => !ValidVerbs.Contains(route.Verb)))
            context.ReportDiagnostic(Diagnostic.Create(MiniApiDiagnostics.InvalidHttpVerb, route.Location, route.MethodName, route.Verb));

        foreach (var route in routes.Where(route => route.Parameters.Count(parameter => parameter.Source == BindingSource.Body) > 1))
            context.ReportDiagnostic(Diagnostic.Create(MiniApiDiagnostics.MultipleBodies, route.Location, route.MethodName));

        foreach (var route in materializedRoutes)
            ReportRouteDiagnostics(context, route);

        foreach (var group in materializedRoutes.Where(IsValidForDuplicateAnalysis).SelectMany(GetRouteKeys).GroupBy(key => key.Value).Where(group => group.Count() > 1))
            foreach (var route in group)
                context.ReportDiagnostic(Diagnostic.Create(MiniApiDiagnostics.DuplicateRoute, route.Model.Location, route.Model.Verb, route.Model.Template, route.Model.Group));

        var validGroups = new HashSet<string>(materializedRoutes.Where(IsValidForDuplicateAnalysis).Select(route => route.Group), StringComparer.OrdinalIgnoreCase);
        foreach (var function in materializedFunctions.Where(function => function.IsDuplicateAssociation))
            context.ReportDiagnostic(Diagnostic.Create(MiniApiDiagnostics.DuplicateMiniApiFunctionAssociation, function.Location, function.FunctionName));

        foreach (var function in materializedFunctions.Where(function => !validGroups.Contains(function.Group)))
            context.ReportDiagnostic(Diagnostic.Create(MiniApiDiagnostics.MissingMiniApiFunction, function.Location, function.FunctionName, function.Group));
    }

    private static void ReportRouteDiagnostics(SourceProductionContext context, RouteModel route)
    {
        foreach (var diagnostic in route.TemplateDiagnostics)
        {
            if (diagnostic.Kind == RouteTemplateDiagnosticKind.UnsupportedConstraint)
                context.ReportDiagnostic(Diagnostic.Create(MiniApiDiagnostics.UnsupportedRouteConstraint, route.Location, diagnostic.Value, diagnostic.Detail));
            else if (diagnostic.Kind == RouteTemplateDiagnosticKind.NonTerminalCatchAll)
                context.ReportDiagnostic(Diagnostic.Create(MiniApiDiagnostics.NonTerminalCatchAll, route.Location, diagnostic.Value));
            else
                context.ReportDiagnostic(Diagnostic.Create(MiniApiDiagnostics.InvalidRouteTemplate, route.Location, route.Template, diagnostic.Detail ?? diagnostic.Value));
        }

        var routeParameterNames = new HashSet<string>(route.Segments.Where(segment => segment.IsParameter).Select(segment => segment.Name), StringComparer.OrdinalIgnoreCase);
        var hasRouteValues = route.Parameters.Any(parameter => parameter.FrameworkKind == FrameworkParameterKind.RouteValues);
        foreach (var token in routeParameterNames.Where(token => !hasRouteValues && !route.Parameters.Any(parameter => parameter.Source == BindingSource.Route && parameter.Name.Equals(token, StringComparison.OrdinalIgnoreCase))))
            context.ReportDiagnostic(Diagnostic.Create(MiniApiDiagnostics.RouteTokenWithoutBinding, route.Location, token));

        var optionalRouteSeen = false;
        foreach (var segment in route.Segments)
        {
            if (segment.IsOptional)
            {
                optionalRouteSeen = true;
                continue;
            }

            if (optionalRouteSeen)
                context.ReportDiagnostic(Diagnostic.Create(MiniApiDiagnostics.InvalidOptionalOrder, route.Location, segment.Name));
        }

        foreach (var parameter in route.Parameters)
        {
            if (parameter.HasBindingConflict)
                context.ReportDiagnostic(Diagnostic.Create(MiniApiDiagnostics.ConflictingBindingAttributes, route.Location, parameter.Name));
            if (parameter.IsFrameworkBody)
                context.ReportDiagnostic(Diagnostic.Create(MiniApiDiagnostics.BodyBindingOnFramework, route.Location, parameter.Name));
            if (parameter.HasUnsupportedCollectionElement)
                context.ReportDiagnostic(Diagnostic.Create(MiniApiDiagnostics.UnsupportedCollectionElement, route.Location, parameter.Name, parameter.Source.ToString().ToLowerInvariant()));
            if (parameter.IsUnbound)
                context.ReportDiagnostic(Diagnostic.Create(MiniApiDiagnostics.UnboundParameter, route.Location, parameter.Name));
        }
    }

    private static bool IsValidForDuplicateAnalysis(RouteModel route)
        => route.Group != "#missing"
            && !route.IsInvalidMethod
            && ValidVerbs.Contains(route.Verb)
            && route.TemplateDiagnostics.Count == 0
            && !HasInvalidOptionalOrder(route)
            && route.Parameters.All(parameter => !parameter.HasBindingConflict && !parameter.IsFrameworkBody && !parameter.HasUnsupportedCollectionElement && !parameter.IsUnbound)
            && route.Parameters.Count(parameter => parameter.Source == BindingSource.Body) <= 1;

    private static bool IsValidForEmission(RouteModel route)
        => IsValidForDuplicateAnalysis(route) && !HasRouteTokenWithoutBinding(route);

    private static bool HasInvalidOptionalOrder(RouteModel route)
    {
        var optionalRouteSeen = false;
        foreach (var segment in route.Segments)
        {
            if (segment.IsOptional)
                optionalRouteSeen = true;
            else if (optionalRouteSeen)
                return true;
        }

        return false;
    }

    private static bool HasRouteTokenWithoutBinding(RouteModel route)
    {
        if (route.Parameters.Any(parameter => parameter.FrameworkKind == FrameworkParameterKind.RouteValues))
            return false;

        return route.Segments.Any(segment 
            => segment.IsParameter
                && !route.Parameters.Any(parameter => parameter.Source == BindingSource.Route && parameter.Name.Equals(segment.Name, StringComparison.OrdinalIgnoreCase)));
    }

    private static IEnumerable<RouteKey> GetRouteKeys(RouteModel route)
    {
        foreach (var routePattern in RouteTemplate.GetEffectiveRoutePatterns(route.Segments))
            yield return new RouteKey($"{route.Group.ToUpperInvariant()}|{route.Verb}|{routePattern}", route);
    }

    private sealed record RouteKey(string Value, RouteModel Model);
}
