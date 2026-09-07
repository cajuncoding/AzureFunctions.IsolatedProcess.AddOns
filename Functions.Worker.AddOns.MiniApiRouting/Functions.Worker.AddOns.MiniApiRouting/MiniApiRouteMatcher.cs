using System.ComponentModel;

namespace Functions.Worker.AddOns.MiniApiRouting;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class MiniApiRouteMatcher
{
    public static bool TryMatch(
        string[] actualSegments,
        MiniApiRouteSegment[] routeSegments,
        int requiredSegments,
        out Dictionary<string, string> routeValues
    )
    {
        routeValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var hasCatchAll = routeSegments.Length > 0 && routeSegments[^1].IsCatchAll;
        var minimumRequiredSegments = hasCatchAll ? Math.Min(requiredSegments, routeSegments.Length - 1) : requiredSegments;

        if (actualSegments.Length < minimumRequiredSegments || (actualSegments.Length > routeSegments.Length && !hasCatchAll))
            return false;

        for (var index = 0; index < routeSegments.Length; index++)
        {
            var expectedSegment = routeSegments[index];

            if (index >= actualSegments.Length)
            {
                if (expectedSegment.IsCatchAll)
                {
                    routeValues[expectedSegment.Name] = string.Empty;
                    return true;
                }

                return index >= requiredSegments;
            }

            if (!expectedSegment.IsParameter)
            {
                if (!expectedSegment.Template.Equals(actualSegments[index], StringComparison.OrdinalIgnoreCase))
                    return false;

                continue;
            }

            var encodedValue = expectedSegment.IsCatchAll ? string.Join("/", actualSegments.Skip(index)) : actualSegments[index];
            var decodedValue = Uri.UnescapeDataString(encodedValue);

            if (!MiniApiRouteConstraint.MatchesConstraint(decodedValue, expectedSegment.Constraint))
                return false;

            routeValues[expectedSegment.Name] = decodedValue;

            if (expectedSegment.IsCatchAll)
                return true;
        }

        return true;
    }
}