namespace Functions.Worker.AddOns.MiniApiRouting.Generators;

internal static class EmitterExtensions
{
    internal static IOrderedEnumerable<RouteModel> OrderByRoutePrecedence(this IEnumerable<RouteModel> routes) => routes
        .OrderBy(route => route.Group, StringComparer.OrdinalIgnoreCase)
        .ThenBy(route => route.Priority)
        .ThenByDescending(GetSpecificity)
        .ThenBy(route => route.Segments.Count)
        .ThenBy(route => route.Template, StringComparer.OrdinalIgnoreCase);

    internal static string ToStringLiteral(this string value)
        => $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";

    private static int GetSpecificity(RouteModel route)
        => route.Segments.Sum(segment => segment.Specificity);
}
