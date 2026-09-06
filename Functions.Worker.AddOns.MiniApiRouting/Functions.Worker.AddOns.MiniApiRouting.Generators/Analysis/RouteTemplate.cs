namespace Functions.Worker.AddOns.MiniApiRouting.Generators;

internal sealed record RouteSegment(
    string Template,
    string Name,
    string? Constraint,
    RouteSegmentKind Kind,
    bool IsOptional
)
{
    internal bool IsParameter => Kind is RouteSegmentKind.Parameter or RouteSegmentKind.CatchAll;
    internal bool IsCatchAll => Kind == RouteSegmentKind.CatchAll;
    internal int Specificity => Kind switch
    {
        RouteSegmentKind.Literal => 100,
        RouteSegmentKind.Parameter when Constraint is not null => 50,
        RouteSegmentKind.Parameter => 10,
        _ => 0
    };
}

internal static class RouteTemplate
{
    internal static string Normalize(string route) => route.NormalizeRoutePath();

    internal static IReadOnlyList<RouteSegment> Parse(
        string route,
        IReadOnlyDictionary<string, ParameterModel> parameters,
        out IReadOnlyList<RouteTemplateDiagnostic> diagnostics
    )
    {
        var result = new List<RouteSegment>();
        var reportedDiagnostics = new List<RouteTemplateDiagnostic>();
        var normalizedRoute = Normalize(route);

        if (normalizedRoute.Length == 0)
        {
            diagnostics = reportedDiagnostics;
            return result;
        }

        var templates = normalizedRoute.SplitRoutePath();
        for (var index = 0; index < templates.Length; index++)
        {
            var segment = ParseSegment(templates[index], parameters, reportedDiagnostics);
            if (segment.IsCatchAll && index != templates.Length - 1)
            {
                reportedDiagnostics.Add(
                    new RouteTemplateDiagnostic(RouteTemplateDiagnosticKind.NonTerminalCatchAll, segment.Name, null)
                );
            }

            result.Add(segment);
        }

        diagnostics = reportedDiagnostics;
        return result;
    }

    internal static IReadOnlyList<string> GetEffectiveRoutePatterns(IReadOnlyList<RouteSegment> segments)
    {
        var routePatterns = new List<string>();
        var optionalStart = segments.Count;
        for (var index = 0; index < segments.Count; index++)
        {
            if (segments[index].IsOptional)
            {
                optionalStart = index;
                break;
            }
        }

        for (var length = optionalStart; length <= segments.Count; length++)
            routePatterns.Add(GetRoutePattern(segments.Take(length)));

        return routePatterns;
    }

    internal static string GetRoutePattern(IEnumerable<RouteSegment> segments)
        => string.Join(RouteTemplateSyntax.PathSeparator, segments.Select(GetSegmentRoutePattern));

    private static RouteSegment ParseSegment(
        string template,
        IReadOnlyDictionary<string, ParameterModel> parameters,
        ICollection<RouteTemplateDiagnostic> diagnostics
    )
    {
        if (!template.IsCompleteParameterSegment())
        {
            if (template.ContainsParameterDelimiter())
            {
                diagnostics.Add(
                    new RouteTemplateDiagnostic(
                    RouteTemplateDiagnosticKind.InvalidSyntax,
                    template,
                    "Route parameters must occupy a complete segment."
                    )
                );
            }

            return new(template, template, null, RouteSegmentKind.Literal, false);
        }

        var token = template.RemoveParameterDelimiters();
        if (token.UsesUnsupportedOptionalOrDefaultSyntax())
        {
            diagnostics.Add(
                new RouteTemplateDiagnostic(
                    RouteTemplateDiagnosticKind.InvalidSyntax,
                    template,
                    "Route parameter syntax supports only {name}, {name:constraint}, and terminal catch-all {*name}."
                )
            );

            return new(template, token, null, RouteSegmentKind.Parameter, false);
        }

        var isCatchAll = token.IsCatchAllToken();
        token = isCatchAll ? token.RemoveCatchAllMarker() : token;

        var parts = token.SplitParameterConstraint();
        var name = parts[0];
        if (name.Length == 0)
        {
            diagnostics.Add(
                new RouteTemplateDiagnostic(
                    RouteTemplateDiagnosticKind.InvalidSyntax,
                    template,
                    "Route parameter name is required."
                )
            );
        }

        if (parts.Length == 2 && !RouteConstraints.IsSupported(parts[1]))
        {
            diagnostics.Add(
                new RouteTemplateDiagnostic(
                    RouteTemplateDiagnosticKind.UnsupportedConstraint,
                    name,
                    parts[1]
                )
            );
        }

        var isOptional = parameters.TryGetValue(name, out var parameter) && parameter.Optional;

        return new(
            template,
            name,
            parts.Length == 2 ? parts[1] : null,
            isCatchAll ? RouteSegmentKind.CatchAll : RouteSegmentKind.Parameter,
            isOptional
        );
    }

    private static string GetSegmentRoutePattern(RouteSegment segment)
    {
        if (!segment.IsParameter)
            return segment.Template.ToLowerInvariant();
        if (segment.IsCatchAll)
            return RouteTemplateSyntax.CatchAllRoutePattern;

        return segment.Constraint is null
            ? RouteTemplateSyntax.ParameterRoutePattern
            : segment.Constraint.ToConstrainedParameterRoutePattern();
    }
}
