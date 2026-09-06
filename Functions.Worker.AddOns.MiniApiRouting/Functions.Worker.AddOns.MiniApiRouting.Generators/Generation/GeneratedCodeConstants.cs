namespace Functions.Worker.AddOns.MiniApiRouting.Generators;

internal static class GeneratedCodeConstants
{
    internal const string BooleanType = "bool";
    internal const string DecimalType = "decimal";
    internal const string GuidType = "Guid";
    internal const string IntegerType = "int";
    internal const string LongType = "long";
    internal const string ValueVariable = "value";
    internal const string DiscardVariable = "_";
    internal const string NumberStylesNumber = "System.Globalization.NumberStyles.Number";
    internal const string CultureInfoInvariantCulture = "System.Globalization.CultureInfo.InvariantCulture";

    internal static string GenerateTryParseExpression(string typeName)
        => $"{typeName}.TryParse({ValueVariable}, out {DiscardVariable})";

    internal static string GenerateDecimalTryParseExpression()
        => $"{DecimalType}.TryParse({ValueVariable}, {NumberStylesNumber}, {CultureInfoInvariantCulture}, out {DiscardVariable})";
}
