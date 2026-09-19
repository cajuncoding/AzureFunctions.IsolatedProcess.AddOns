using System;
using System.Globalization;

internal static class MiniApiRouteConstraint
{
    internal const string StringConstraint = "string";
    internal const string IntConstraint = "int";
    internal const string LongConstraint = "long";
    internal const string GuidConstraint = "guid";
    internal const string BoolConstraint = "bool";
    internal const string DecimalConstraint = "decimal";

    internal static readonly string[] All =
    [
        StringConstraint,
        IntConstraint,
        LongConstraint,
        GuidConstraint,
        BoolConstraint,
        DecimalConstraint
    ];

    //NOTE: We have explicit case matching as this is the Hot Path of the MiniApi Routing/Dispatching
    //      and we want to avoid the overhead of a Dictionary or HashSet lookup, allocations, etc...
    internal static bool IsSupported(string constraintName)
        => constraintName.Equals(StringConstraint, StringComparison.OrdinalIgnoreCase)
        || constraintName.Equals(IntConstraint, StringComparison.OrdinalIgnoreCase)
        || constraintName.Equals(LongConstraint, StringComparison.OrdinalIgnoreCase)
        || constraintName.Equals(GuidConstraint, StringComparison.OrdinalIgnoreCase)
        || constraintName.Equals(BoolConstraint, StringComparison.OrdinalIgnoreCase)
        || constraintName.Equals(DecimalConstraint, StringComparison.OrdinalIgnoreCase);

    //NOTE: We have explicit if/else case matching as this is the Hot Path of the MiniApi Routing/Dispatching
    //      and we want to avoid the overhead of a Dictionary or HashSet lookup, allocations, etc...
    internal static bool MatchesConstraint(string value, string? constraintName)
    {
        if (string.IsNullOrWhiteSpace(constraintName))
            return true;

        if (constraintName!.Equals(StringConstraint, StringComparison.OrdinalIgnoreCase))
            return true;

        if (constraintName!.Equals(IntConstraint, StringComparison.OrdinalIgnoreCase))
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);

        if (constraintName!.Equals(LongConstraint, StringComparison.OrdinalIgnoreCase))
            return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);

        if (constraintName!.Equals(GuidConstraint, StringComparison.OrdinalIgnoreCase))
            return Guid.TryParse(value, out _);

        if (constraintName!.Equals(BoolConstraint, StringComparison.OrdinalIgnoreCase))
            return bool.TryParse(value, out _);

        if (constraintName!.Equals(DecimalConstraint, StringComparison.OrdinalIgnoreCase))
            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _);

        return false;
    }
}