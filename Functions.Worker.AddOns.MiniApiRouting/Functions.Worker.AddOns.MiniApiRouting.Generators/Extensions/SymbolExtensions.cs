using Microsoft.CodeAnalysis;

namespace Functions.Worker.AddOns.MiniApiRouting.Generators;

internal static class SymbolExtensions
{
    internal static string GetFullyQualifiedName(this ITypeSymbol symbol)
        => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    internal static AttributeData? GetAttribute(this ISymbol symbol, string metadataName)
        => symbol.GetAttributes().FirstOrDefault(attribute => attribute.AttributeClass?.ToDisplayString() == metadataName);

    internal static string? GetConstructorString(this AttributeData attribute, int index = 0)
        => attribute.ConstructorArguments.Length > index ? attribute.ConstructorArguments[index].Value as string : null;

    internal static int GetNamedInt(this AttributeData attribute, string name, int fallback)
    {
        var argument = attribute.NamedArguments.FirstOrDefault(argument => argument.Key == name);
        return argument.Value.Value is int value ? value : fallback;
    }

    internal static FrameworkParameterKind GetFrameworkParameterKind(this IParameterSymbol parameter)
    {
        return parameter.Type.GetMetadataTypeName() switch
        {
            WellKnownMetadataNames.HttpRequestData => FrameworkParameterKind.Request,
            WellKnownMetadataNames.FunctionContext => FrameworkParameterKind.Context,
            WellKnownMetadataNames.CancellationToken => FrameworkParameterKind.CancellationToken,
            WellKnownMetadataNames.MiniApiRouteValues => FrameworkParameterKind.RouteValues,
            _ => FrameworkParameterKind.None
        };
    }

    internal static bool IsScalar(this ITypeSymbol type)
    {
        var typeName = type.GetMetadataTypeName();
        return type.SpecialType is SpecialType.System_String
            or SpecialType.System_Boolean
            or SpecialType.System_Byte
            or SpecialType.System_Int16
            or SpecialType.System_Int32
            or SpecialType.System_Int64
            or SpecialType.System_Single
            or SpecialType.System_Double
            or SpecialType.System_Char
            || type.TypeKind == TypeKind.Enum
            || WellKnownScalarTypeNames.Contains(typeName);
    }

    internal static bool IsImplicitBodyCandidate(this ITypeSymbol type)
    {
        var typeName = type.GetMetadataTypeName();
        return !type.IsScalar()
            && !ImplicitBodyExcludedTypeNames.Contains(typeName)
            && type.TypeKind != TypeKind.Interface;
    }

    internal static bool IsAccessibleToGeneratedCode(this Accessibility accessibility)
        => accessibility is Accessibility.Public or Accessibility.Internal;

    private static string GetMetadataTypeName(this ITypeSymbol type)
        => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", string.Empty);

    internal static bool InheritsFrom(this INamedTypeSymbol? type, string metadataName)
    {
        while (type is not null)
        {
            if (type.ToDisplayString() == metadataName)
                return true;

            type = type.BaseType;
        }

        return false;
    }

    private static readonly HashSet<string> WellKnownScalarTypeNames = new(StringComparer.Ordinal)
    {
        WellKnownMetadataNames.SystemGuid,
        WellKnownMetadataNames.SystemDateTime,
        WellKnownMetadataNames.SystemDateTimeOffset,
        WellKnownMetadataNames.SystemDecimal,
        WellKnownMetadataNames.SystemTimeSpan
    };

    private static readonly HashSet<string> ImplicitBodyExcludedTypeNames = new(StringComparer.Ordinal)
    {
        WellKnownMetadataNames.Object,
        WellKnownMetadataNames.SystemObject,
        WellKnownMetadataNames.SystemStream,
        WellKnownMetadataNames.SystemBinaryData
    };
}
