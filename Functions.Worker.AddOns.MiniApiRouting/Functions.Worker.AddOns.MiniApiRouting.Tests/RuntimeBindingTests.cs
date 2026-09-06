using Xunit;

namespace Functions.Worker.AddOns.MiniApiRouting.Tests;

public sealed class RuntimeBindingTests
{
    [Theory]
    [InlineData("42", 42)]
    [InlineData("-7", -7)]
    public void ConvertsIntegers(string value, int expected)
    {
        Assert.Equal(expected, MiniApiValueConverter.Convert<int>(value, "id", "route"));
    }

    [Fact]
    public void ConvertsGuid()
    {
        var expected = Guid.NewGuid();

        Assert.Equal(expected, MiniApiValueConverter.Convert<Guid>(expected.ToString(), "id", "route"));
    }

    [Fact]
    public void ConvertsEnumsCaseInsensitively()
    {
        Assert.Equal(StringComparison.OrdinalIgnoreCase, MiniApiValueConverter.Convert<StringComparison>("ordinalignorecase", "comparison", "query"));
    }

    [Fact]
    public void ConvertsDefinedNumericEnumValues()
    {
        Assert.Equal(WidgetStatus.Active, MiniApiValueConverter.Convert<WidgetStatus>("1", "status", "query"));
    }

    [Theory]
    [InlineData("999")]
    [InlineData("Deleted")]
    public void RejectsUndefinedEnumValues(string value)
    {
        var exception = Assert.Throws<MiniApiParameterBindingException>(() => MiniApiValueConverter.Convert<WidgetStatus>(value, "status", "query"));

        Assert.Equal("status", exception.ParameterName);
        Assert.NotNull(exception.InnerException);
    }

    [Fact]
    public void ConversionFailureIncludesBindingContext()
    {
        var exception = Assert.Throws<MiniApiParameterBindingException>(() => MiniApiValueConverter.Convert<int>("abc", "id", "route"));

        Assert.Equal("id", exception.ParameterName);
        Assert.Equal("route", exception.BindingSource);
        Assert.Equal(typeof(int), exception.TargetType);
        Assert.Equal("abc", exception.Value);
        Assert.NotNull(exception.InnerException);
    }

    [Fact]
    public void RouteValuesUseOrdinalCaseInsensitiveKeys()
    {
        var routeValues = new MiniApiRouteValues(new Dictionary<string, string>
        {
            ["WidgetId"] = "42"
        });

        Assert.True(routeValues.ContainsKey("widgetid"));
        Assert.True(routeValues.TryGetValue("WIDGETID", out var value));
        Assert.Equal("42", value);
    }

    [Fact]
    public void RouteNotFoundExceptionExposesRouteContext()
    {
        var exception = new MiniApiRouteNotFoundException("GET", "widgets/42");

        Assert.Equal("GET", exception.Verb);
        Assert.Equal("widgets/42", exception.Route);
        Assert.Contains("widgets/42", exception.Message);
    }

    private enum WidgetStatus
    {
        Active = 1,
        Archived = 2
    }
}
