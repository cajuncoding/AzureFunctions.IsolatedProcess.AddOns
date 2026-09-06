namespace Functions.Worker.AddOns.MiniApiRouting.Generators;

internal static class RouteTemplateSyntax
{
    internal const string PathSeparator = "/";
    internal const string CatchAllRoutePattern = "{*}";
    internal const string ParameterRoutePattern = "{}";
    internal const string EmptyConstraint = "";

    private const char PathSeparatorCharacter = '/';
    private const char ParameterOpenCharacter = '{';
    private const char ParameterCloseCharacter = '}';
    private const char CatchAllCharacter = '*';
    private const char ConstraintSeparatorCharacter = ':';
    private const char OptionalCharacter = '?';
    private const char DefaultValueCharacter = '=';

    internal static string NormalizeRoutePath(this string route)
        => route.Trim().Trim(PathSeparatorCharacter);

    internal static string[] SplitRoutePath(this string route)
        => route.Split(new[] { PathSeparatorCharacter }, StringSplitOptions.RemoveEmptyEntries);

    internal static bool IsCompleteParameterSegment(this string segment)
        => segment.StartsWith(ParameterOpenCharacter.ToString(), StringComparison.Ordinal)
            && segment.EndsWith(ParameterCloseCharacter.ToString(), StringComparison.Ordinal);

    internal static bool ContainsParameterDelimiter(this string segment)
        => segment.IndexOf(ParameterOpenCharacter) >= 0 || segment.IndexOf(ParameterCloseCharacter) >= 0;

    internal static string RemoveParameterDelimiters(this string segment)
        => segment.Substring(1, segment.Length - 2);

    internal static bool UsesUnsupportedOptionalOrDefaultSyntax(this string token)
        => token.Length == 0
            || token.IndexOf(OptionalCharacter) >= 0
            || token.IndexOf(DefaultValueCharacter) >= 0;

    internal static bool IsCatchAllToken(this string token)
        => token.StartsWith(CatchAllCharacter.ToString(), StringComparison.Ordinal);

    internal static string RemoveCatchAllMarker(this string token)
        => token.TrimStart(CatchAllCharacter);

    internal static string[] SplitParameterConstraint(this string token)
        => token.Split(new[] { ConstraintSeparatorCharacter }, 2);

    internal static string ToConstrainedParameterRoutePattern(this string constraint)
        => $"{{:{constraint.ToLowerInvariant()}}}";
}
