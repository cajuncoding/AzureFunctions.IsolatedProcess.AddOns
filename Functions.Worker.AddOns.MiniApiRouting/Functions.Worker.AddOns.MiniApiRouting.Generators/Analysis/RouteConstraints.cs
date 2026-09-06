namespace Functions.Worker.AddOns.MiniApiRouting.Generators;

internal static class RouteConstraints
{
    internal static readonly IReadOnlyList<RouteConstraintDefinition> SupportedRouteConstraints = new[]
    {
        new RouteConstraintDefinition("int", GeneratedCodeReferences.TryParseExpression(GeneratedCodeReferences.IntegerType)),
        new RouteConstraintDefinition("long", GeneratedCodeReferences.TryParseExpression(GeneratedCodeReferences.LongType)),
        new RouteConstraintDefinition("guid", GeneratedCodeReferences.TryParseExpression(GeneratedCodeReferences.GuidType)),
        new RouteConstraintDefinition("bool", GeneratedCodeReferences.TryParseExpression(GeneratedCodeReferences.BooleanType)),
        new RouteConstraintDefinition("decimal", GeneratedCodeReferences.DecimalTryParseExpression())
    };

    private static readonly HashSet<string> SupportedRouteConstraintNames = new HashSet<string>(
        SupportedRouteConstraints.Select(constraint => constraint.Name),
        StringComparer.OrdinalIgnoreCase
    );

    internal static bool IsSupported(string constraint)
        => SupportedRouteConstraintNames.Contains(constraint);
}

internal sealed record RouteConstraintDefinition(string Name, string GeneratedTryParseExpression);
