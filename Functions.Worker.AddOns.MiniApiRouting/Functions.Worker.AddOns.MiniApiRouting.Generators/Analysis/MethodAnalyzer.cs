using System.Globalization;
using Microsoft.CodeAnalysis;

namespace Functions.Worker.AddOns.MiniApiRouting.Generators;

internal static class MethodAnalyzer
{
    internal static IReadOnlyList<ParameterModel> AnalyzeParameters(IMethodSymbol method, string route)
    {
        var routeNames = ParseRouteParameterNames(route);
        return method.Parameters.Select(parameter => AnalyzeParameter(parameter, routeNames)).ToArray();
    }

    internal static bool IsInvalidRouteHandlerMethod(IMethodSymbol method)
        => method.IsAbstract
            || method.IsGenericMethod
            || !method.DeclaredAccessibility.IsAccessibleToGeneratedCode()
            || !method.ContainingType.DeclaredAccessibility.IsAccessibleToGeneratedCode();

    internal static ReturnKind AnalyzeReturnKind(IMethodSymbol method)
    {
        if (method.ReturnsVoid)
            return ReturnKind.Void;
        if (method.ReturnType is not INamedTypeSymbol returnType || returnType.Name is not (nameof(Task) or nameof(ValueTask)))
            return ReturnKind.Value;
        if (returnType.TypeArguments.Length == 0)
            return returnType.Name == nameof(Task) ? ReturnKind.Task : ReturnKind.ValueTask;
        
        return returnType.Name == nameof(Task) ? ReturnKind.TaskValue : ReturnKind.ValueTaskValue;
    }

    private static ParameterModel AnalyzeParameter(IParameterSymbol parameter, HashSet<string> routeNames)
    {
        var headerAttribute = parameter.GetAttribute(WellKnownMetadataNames.MiniApiFromHeaderAttribute);
        var bodyAttribute = parameter.GetAttribute(WellKnownMetadataNames.MiniApiFromBodyAttribute);
        var collection = GetCollection(parameter.Type, out var elementType);
        var bindingType = elementType ?? parameter.Type;
        var frameworkKind = parameter.GetFrameworkParameterKind();
        var hasBindingConflict = headerAttribute is not null && bodyAttribute is not null;
        var source = GetBindingSource(parameter, bindingType, routeNames, headerAttribute, bodyAttribute, frameworkKind, hasBindingConflict);
        var optional = parameter.NullableAnnotation == NullableAnnotation.Annotated || parameter.HasExplicitDefaultValue;
        var defaultValue = parameter.HasExplicitDefaultValue ? GetLiteral(parameter.ExplicitDefaultValue) : null;
        var hasUnsupportedCollectionElement = collection != CollectionKind.None && !bindingType.IsScalar();
        var isFrameworkBody = frameworkKind != FrameworkParameterKind.None && bodyAttribute is not null;
        var isUnbound = source == BindingSource.Unbound;

        return new(
            parameter.Name,
            parameter.Type.GetFullyQualifiedName(),
            elementType?.GetFullyQualifiedName(),
            source,
            optional,
            parameter.NullableAnnotation == NullableAnnotation.Annotated,
            headerAttribute?.GetConstructorString(),
            defaultValue,
            collection,
            frameworkKind,
            hasBindingConflict,
            isFrameworkBody,
            hasUnsupportedCollectionElement,
            isUnbound
        );
    }

    private static BindingSource GetBindingSource(
        IParameterSymbol parameter,
        ITypeSymbol bindingType,
        HashSet<string> routeNames,
        AttributeData? headerAttribute,
        AttributeData? bodyAttribute,
        FrameworkParameterKind frameworkKind,
        bool hasBindingConflict
    )
    {
        if (hasBindingConflict)
            return BindingSource.Unbound;
        if (frameworkKind != FrameworkParameterKind.None)
            return BindingSource.Framework;
        if (headerAttribute is not null)
            return BindingSource.Header;
        if (bodyAttribute is not null)
            return BindingSource.Body;
        if (routeNames.Contains(parameter.Name))
            return BindingSource.Route;
        if (bindingType.IsScalar())
            return BindingSource.Query;
        if (bindingType.IsImplicitBodyCandidate())
            return BindingSource.Body;

        return BindingSource.Unbound;
    }

    private static CollectionKind GetCollection(ITypeSymbol type, out ITypeSymbol? elementType)
    {
        elementType = null;
        if (type is IArrayTypeSymbol array)
        {
            elementType = array.ElementType;
            return CollectionKind.Array;
        }

        if (type is not INamedTypeSymbol namedType || namedType.TypeArguments.Length != 1)
            return CollectionKind.None;

        if (namedType.Name is not ("List" or "IList" or "IReadOnlyList" or "IEnumerable" or "ICollection" or "IReadOnlyCollection"))
            return CollectionKind.None;

        elementType = namedType.TypeArguments[0];
        return namedType.Name == "List" ? CollectionKind.List : CollectionKind.Interface;
    }

    private static HashSet<string> ParseRouteParameterNames(string route) => new HashSet<string>(RouteTemplate.Normalize(route)
        .Split('/')
        .Where(segment => segment.StartsWith("{") && segment.EndsWith("}"))
        .Select(segment => segment.Trim('{', '}').TrimStart('*').Split(':')[0]),
        StringComparer.OrdinalIgnoreCase
    );

    private static string? GetLiteral(object? value) => value switch
    {
        null => "null",
        string text => "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"",
        bool flag => flag ? "true" : "false",
        char character => "'" + character.ToString().Replace("\\", "\\\\").Replace("'", "\\'") + "'",
        _ => Convert.ToString(value, CultureInfo.InvariantCulture)
    };
}
