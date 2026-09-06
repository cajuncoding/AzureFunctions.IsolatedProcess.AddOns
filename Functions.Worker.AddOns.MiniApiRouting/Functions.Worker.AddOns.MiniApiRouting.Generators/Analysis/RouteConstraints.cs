namespace Functions.Worker.AddOns.MiniApiRouting.Generators;

internal static class RouteConstraints
{
    internal static readonly IReadOnlyList<RouteConstraintDefinition> SupportedRouteConstraints = new[]
    {
        new RouteConstraintDefinition("int", GeneratedCodeConstants.GenerateTryParseExpression(GeneratedCodeConstants.IntegerType)),
        new RouteConstraintDefinition("long", GeneratedCodeConstants.GenerateTryParseExpression(GeneratedCodeConstants.LongType)),
        new RouteConstraintDefinition("guid", GeneratedCodeConstants.GenerateTryParseExpression(GeneratedCodeConstants.GuidType)),
        new RouteConstraintDefinition("bool", GeneratedCodeConstants.GenerateTryParseExpression(GeneratedCodeConstants.BooleanType)),
        new RouteConstraintDefinition("decimal", GeneratedCodeConstants.GenerateDecimalTryParseExpression())
    };

    private static readonly HashSet<string> SupportedRouteConstraintNames = new HashSet<string>(
        SupportedRouteConstraints.Select(constraint => constraint.Name),
        StringComparer.OrdinalIgnoreCase
    );

    internal static bool IsSupported(string constraint)
        => SupportedRouteConstraintNames.Contains(constraint);
}

internal sealed record RouteConstraintDefinition(string Name, string GeneratedTryParseExpression);
