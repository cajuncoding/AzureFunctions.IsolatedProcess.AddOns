using Functions.Worker.AddOns.MiniApiRouting.Generators;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using Xunit;

namespace Functions.Worker.AddOns.MiniApiRouting.Generator.Tests;

public sealed class MiniApiRoutingGeneratorTests
{
    [Theory]
    [MemberData(nameof(DiagnosticCases))]
    public void ReportsExpectedDiagnostic(string diagnosticId, string source)
    {
        var result = RunGenerator(source);

        var diagnostics = result.Diagnostics
            .Where(diagnostic => diagnostic.Id == diagnosticId)
            .ToArray();

        Assert.NotEmpty(diagnostics);

        Assert.All(
            diagnostics,
            diagnostic =>
            {
                Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
                Assert.False(string.IsNullOrWhiteSpace(diagnostic.GetMessage()));
                Assert.NotEqual(Location.None, diagnostic.Location);
            }
        );
    }

    [Fact]
    public void ValidCodeEmitsReadableRouterSource()
    {
        var source = $$"""
            {{Header}}

            internal static class MiniApis
            {
                internal const string Widgets = "widgets";
            }

            [MiniApi(MiniApis.Widgets)]
            internal sealed class WidgetHandlers
            {
                [MiniApiRouteHandler(MiniApiVerbs.Get, "/{id:int}", Priority = 10)]
                public static Task<string> GetAsync(int id, string? filter = null, CancellationToken cancellationToken = default)
                    => Task.FromResult(id.ToString());

                [MiniApiRouteHandler(MiniApiVerbs.Get, "/assets/{*path}")]
                public static string Asset(string path) => path;
            }

            internal sealed class WidgetFunction
            {
                [Function(nameof(WidgetFunction))]
                [MiniApiFunction(MiniApis.Widgets)]
                public void Run() { }
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        var generated = Assert.Single(result.GeneratedSources.Where(source => source.HintName == "MiniApiGenerated.g.cs"));
        var text = generated.SourceText.ToString();
        Assert.Contains("internal sealed class GeneratedMiniApiRouter", text);
        Assert.Contains("TryMatch", text);
        Assert.Contains("internal static class GeneratedMiniApiServiceRegistration", text);
        Assert.Contains("[ModuleInitializer]", text);
        Assert.Contains("MiniApiServiceCollectionExtensions.RegisterGeneratedMiniApiRouting(AddGeneratedMiniApiRouting)", text);
        Assert.Contains("AddGeneratedMiniApiRouting", text);
        Assert.Contains("request.FunctionContext.InstanceServices", text);
        Assert.Contains("services.TryAddSingleton<IMiniApiRouter, GeneratedMiniApiRouter>()", text);
        Assert.Contains("new MiniApiGeneratedRouteSegment(\"{id:int}\"", text);
        Assert.DoesNotContain("new MiniApiGeneratedRouteSegment(\"/{id:int}\"", text);
        Assert.Contains("WidgetHandlers.GetAsync", text);
    }

    [Fact]
    public void ValidDefaultAndNamedGroupsDoNotReportDiagnostics()
    {
        var source = $$"""
            {{Header}}

            [MiniApi]
            internal sealed class DefaultHandlers
            {
                [MiniApiRouteHandler(MiniApiVerbs.Get)]
                public static string Get() => "ok";
            }

            [MiniApi("books")]
            internal sealed class BookHandlers
            {
                [MiniApiRouteHandler(MiniApiVerbs.Get, "/{category}/{identifier}")]
                public static string Search(string? category = null, string? identifier = null) => "ok";
            }

            internal sealed class DefaultFunction
            {
                [Function(nameof(DefaultFunction))]
                [MiniApiFunction]
                public void Run() { }
            }

            internal sealed class BookFunction
            {
                [Function(nameof(BookFunction))]
                [MiniApiFunction("books")]
                public void Run() { }
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.Contains(result.GeneratedSources, source => source.HintName == "MiniApiGenerated.g.cs");
    }

    [Fact]
    public void LiteralAfterOptionalRouteParameterReportsMAR005()
    {
        var source = $$"""
        {{Header}}

        [MiniApi]
        internal sealed class WidgetHandlers
        {
            [MiniApiRouteHandler(MiniApiVerbs.Get, "/{widgetId}/details")]
            public static string GetWidgetDetails(string? widgetId = null)
                => "ok";
        }
        """;

        var result = RunGenerator(source);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "MAR005");
    }

    [Fact]
    public void TerminalOptionalRouteParameterDoesNotReportMAR005()
    {
        var source = $$"""
        {{Header}}

        [MiniApi]
        internal sealed class WidgetHandlers
        {
            [MiniApiRouteHandler(MiniApiVerbs.Get, "/{widgetId}")]
            public static string GetWidget(string? widgetId = null)
                => "ok";
        }
        """;

        var result = RunGenerator(source);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Id == "MAR005");
    }

    [Fact]
    public void OptionalRouteParameterConflictingWithRootRouteReportsMAR002()
    {
        var source = $$"""
        {{Header}}

        [MiniApi]
        internal sealed class WidgetHandlers
        {
            [MiniApiRouteHandler(MiniApiVerbs.Get)]
            public static string GetWidgets()
                => "ok";

            [MiniApiRouteHandler(MiniApiVerbs.Get, "/{widgetId}")]
            public static string GetWidget(string? widgetId = null)
                => "ok";
        }
        """;

        var result = RunGenerator(source);

        var diagnostics = result.Diagnostics
            .Where(diagnostic => diagnostic.Id == "MAR002")
            .ToArray();

        // The generator reports MAR002 against both conflicting handlers:
        // GET "" and GET "/{widgetId}".
        Assert.Equal(2, diagnostics.Length);
    }

    public static IEnumerable<object[]> DiagnosticCases()
    {
        yield return Case("MAR001", """internal sealed class Handlers { [MiniApiRouteHandler(MiniApiVerbs.Get)] public static string Get() => "ok"; }""");
        yield return Case("MAR002", """[MiniApi] internal sealed class Handlers { [MiniApiRouteHandler(MiniApiVerbs.Get, "/{id:int}")] public static string Get(int id) => "ok"; [MiniApiRouteHandler(MiniApiVerbs.Get, "/{other:int}")] public static string GetOther(int other) => "ok"; }""");
        yield return Case("MAR003", """[MiniApi] internal abstract class Handlers { [MiniApiRouteHandler(MiniApiVerbs.Get)] public abstract string Get(); }""");
        yield return Case("MAR004", """internal sealed class RequestA { } internal sealed class RequestB { } [MiniApi] internal sealed class Handlers { [MiniApiRouteHandler(MiniApiVerbs.Post)] public static string Post(RequestA a, RequestB b) => "ok"; }""");
        yield return Case("MAR005", """[MiniApi] internal sealed class Handlers { [MiniApiRouteHandler(MiniApiVerbs.Get, "/{category}/{id}")] public static string Get(string? category, int id) => "ok"; }""");
        yield return Case("MAR006", """internal sealed class FunctionHost { [Function(nameof(FunctionHost))] [MiniApiFunction("missing")] public void Run() { } }""");
        yield return Case("MAR007", """[MiniApi] internal sealed class Handlers { [MiniApiRouteHandler(MiniApiVerbs.Get)] public static string Get(System.IO.Stream stream) => "ok"; }""");
        yield return Case("MAR008", """[MiniApi] internal sealed class Handlers { [MiniApiRouteHandler(MiniApiVerbs.Get, "/{id:datetime}")] public static string Get(string id) => "ok"; }""");
        yield return Case("MAR009", """[MiniApi] internal sealed class Handlers { [MiniApiRouteHandler(MiniApiVerbs.Get, "/assets/{*path}/name")] public static string Get(string path) => "ok"; }""");
        yield return Case("MAR010", """[MiniApi] internal sealed class Handlers { [MiniApiRouteHandler(MiniApiVerbs.Get)] public static string Get([MiniApiFromBody][MiniApiFromHeader("x-id")] string value) => "ok"; }""");
        yield return Case("MAR011", """[MiniApi] internal sealed class Handlers { [MiniApiRouteHandler(MiniApiVerbs.Get, "/{id:int}")] public static string Get() => "ok"; }""");
        yield return Case("MAR012", """[MiniApi] internal sealed class Handlers { [MiniApiRouteHandler(MiniApiVerbs.Post)] public static string Post([MiniApiFromBody] HttpRequestData request) => "ok"; }""");
        yield return Case("MAR013", """internal sealed class Widget { } [MiniApi] internal sealed class Handlers { [MiniApiRouteHandler(MiniApiVerbs.Get)] public static string Get([MiniApiFromHeader("x-widget")] List<Widget> widgets) => "ok"; }""");
        yield return Case("MAR015", """[MiniApi] internal sealed class Handlers { [MiniApiRouteHandler("BOGUS")] public static string Get() => "ok"; }""");
        yield return Case("MAR016", """[MiniApi] internal sealed class Handlers { [MiniApiRouteHandler(MiniApiVerbs.Get, "/{id?}")] public static string Get(string id) => "ok"; }""");
    }

    private static object[] Case(string diagnosticId, string body) => new object[] { diagnosticId, $"{Header}\n{body}" };

    private static GeneratorResult RunGenerator(string source)
    {
        var compilation = CSharpCompilation.Create(
            "Consumer",
            new[] { CSharpSyntaxTree.ParseText(SourceText.From(source), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest)) },
            CreateReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable)
        );

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new MiniApiRoutingGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);
        var runResult = driver.GetRunResult();
        var diagnostics = outputCompilation.GetDiagnostics().Concat(generatorDiagnostics).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToImmutableArray();

        return new GeneratorResult(diagnostics, runResult.Results.SelectMany(result => result.GeneratedSources).ToImmutableArray());
    }

    private static IEnumerable<MetadataReference> CreateReferences()
    {
        var trustedAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))?.Split(Path.PathSeparator) ?? Array.Empty<string>();
        foreach (var assemblyPath in trustedAssemblies)
            yield return MetadataReference.CreateFromFile(assemblyPath);

        yield return MetadataReference.CreateFromFile(typeof(MiniApiAttribute).Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(Microsoft.Azure.Functions.Worker.FunctionAttribute).Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(HttpRequestData).Assembly.Location);
    }

    private const string Header = """
        using System;
        using System.Collections.Generic;
        using System.Threading;
        using System.Threading.Tasks;
        using Functions.Worker.AddOns.MiniApiRouting;
        using Microsoft.Azure.Functions.Worker;
        using Microsoft.Azure.Functions.Worker.Http;
        """;

    private sealed record GeneratorResult(ImmutableArray<Diagnostic> Diagnostics, ImmutableArray<GeneratedSourceResult> GeneratedSources);
}
