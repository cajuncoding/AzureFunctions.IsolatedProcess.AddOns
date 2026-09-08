using Xunit;

namespace Functions.Worker.AddOns.MiniApiRouting.Tests;

public sealed class MiniApiRouteMatcherTests
{
    [Theory]
    [InlineData("My%20Widget", "My Widget")]
    [InlineData("Caf%C3%A9", "Café")]
    [InlineData("%E6%97%A5%E6%9C%AC", "日本")]
    public void TryMatchDecodesEncodedRouteParameterBeforeBinding(string encodedValue, string expectedValue)
    {
        // These cases protect MiniApiRouting's route-matching behavior,
        // not Uri.UnescapeDataString() itself. The matcher must decode the
        // captured value before exposing it to generated parameter binding.
        //
        // Spaces, accented characters, and multibyte Unicode are included
        // because encoded path handling is easily lost during matcher refactoring.

        var routeSegments = new[]
        {
            new MiniApiRouteSegment("{widgetName}", "widgetName", null, false, true)
        };

        var matched = MiniApiRouteMatcher.TryMatch(
            new[] { encodedValue },
            routeSegments,
            1,
            out var routeValues
        );

        Assert.True(matched);
        Assert.Equal(expectedValue, routeValues["widgetName"]);
    }

    [Fact]
    public void TryMatchDecodesEncodedSlashAfterSegmentMatching()
    {
        // An encoded slash must remain inside one route segment while matching
        // and decode only after that segment has been captured.
        //
        // This protects the current behavior where "abc%2Fdef" binds as
        // "abc/def" without being incorrectly split into two input segments.

        var routeSegments = new[]
        {
            new MiniApiRouteSegment("{widgetName}", "widgetName", null, false, true)
        };

        var matched = MiniApiRouteMatcher.TryMatch(
            new[] { "abc%2Fdef" },
            routeSegments,
            1,
            out var routeValues
        );

        Assert.True(matched);
        Assert.Equal("abc/def", routeValues["widgetName"]);
    }

    [Fact]
    public void TryMatchDecodesCatchAllRouteValue()
    {
        // Catch-all values are assembled from all remaining encoded path
        // segments and then decoded into one bound route value.

        var routeSegments = new[]
        {
            new MiniApiRouteSegment("assets", "assets", null, false, false),
            new MiniApiRouteSegment("{*path}", "path", null, true, true)
        };

        var matched = MiniApiRouteMatcher.TryMatch(
            new[] { "assets", "Caf%C3%A9", "abc%2Fdef" },
            routeSegments,
            2,
            out var routeValues
        );

        Assert.True(matched);
        Assert.Equal("Café/abc/def", routeValues["path"]);
    }

    [Fact]
    public void TryMatchAllowsEmptyCatchAllRouteValue()
    {
        // A catch-all represents the remaining path, which may be empty.
        // This locks down the expected behavior that "/assets" matches
        // "/assets/{*path}" and binds path to an empty string.

        var routeSegments = new[]
        {
            new MiniApiRouteSegment("assets", "assets", null, false, false),
            new MiniApiRouteSegment("{*path}", "path", null, true, true)
        };

        var matched = MiniApiRouteMatcher.TryMatch(
            new[] { "assets" },
            routeSegments,
            2,
            out var routeValues
        );

        Assert.True(matched);
        Assert.Equal(string.Empty, routeValues["path"]);
    }

    [Fact]
    public void TryMatchAllowsEmptyRootCatchAllRouteValue()
    {
        // A root catch-all also represents an optional path remainder.
        // An empty request path must match and bind the catch-all to an empty string.

        var routeSegments = new[]
        {
            new MiniApiRouteSegment("{*path}", "path", null, true, true)
        };

        var matched = MiniApiRouteMatcher.TryMatch(
            Array.Empty<string>(),
            routeSegments,
            1,
            out var routeValues
        );

        Assert.True(matched);
        Assert.Equal(string.Empty, routeValues["path"]);
    }

    [Fact]
    public void TryMatchMatchesEmptyRootRoute()
    {
        // A route with no configured segments represents the Mini API root.
        // This protects root-route matching independently from catch-all behavior.

        var matched = MiniApiRouteMatcher.TryMatch(
            Array.Empty<string>(),
            Array.Empty<MiniApiRouteSegment>(),
            0,
            out var routeValues
        );

        Assert.True(matched);
        Assert.Empty(routeValues);
    }

    [Fact]
    public void TryMatchRejectsNonEmptyPathForEmptyRootRoute()
    {
        var matched = MiniApiRouteMatcher.TryMatch(
            new[] { "widgets" },
            Array.Empty<MiniApiRouteSegment>(),
            0,
            out var routeValues
        );

        Assert.False(matched);
        Assert.Empty(routeValues);
    }

    [Fact]
    public void TryMatchEvaluatesConstraintAfterDecoding()
    {
        // Constraint evaluation must use the decoded route value. This ensures
        // encoded numeric characters still satisfy the declared int constraint.

        var routeSegments = new[]
        {
            new MiniApiRouteSegment("{widgetId:int}", "widgetId", "int", false, true)
        };

        var matched = MiniApiRouteMatcher.TryMatch(
            new[] { "%34%32" },
            routeSegments,
            1,
            out var routeValues
        );

        Assert.True(matched);
        Assert.Equal("42", routeValues["widgetId"]);
    }

    [Theory]
    [InlineData("string", "anything", true)]
    [InlineData("string", "", true)]
    [InlineData("int", "42", true)]
    [InlineData("int", "-42", true)]
    [InlineData("int", "2147483648", false)]
    [InlineData("int", "not-an-integer", false)]
    [InlineData("long", "9223372036854775807", true)]
    [InlineData("long", "9223372036854775808", false)]
    [InlineData("long", "not-a-long", false)]
    [InlineData("guid", "98bdf731-098f-4f65-89e6-e62745ec0bdb", true)]
    [InlineData("guid", "not-a-guid", false)]
    [InlineData("bool", "true", true)]
    [InlineData("bool", "FALSE", true)]
    [InlineData("bool", "1", false)]
    [InlineData("bool", "not-a-boolean", false)]
    [InlineData("decimal", "1234.56", true)]
    [InlineData("decimal", "-1234.56", true)]
    [InlineData("decimal", "1,234.56", true)]
    [InlineData("decimal", "not-a-decimal", false)]
    public void TryMatchEvaluatesEverySupportedConstraint(string constraint, string value, bool expected)
    {
        // This test keeps runtime matching coverage aligned with the shared
        // MiniApiRouteConstraint definitions used by both runtime and generator
        // projects. Every supported constraint must accept and reject values
        // according to its shared matching function.

        var routeSegments = new[]
        {
            new MiniApiRouteSegment($"{{value:{constraint}}}", "value", constraint, false, true)
        };

        var matched = MiniApiRouteMatcher.TryMatch(
            new[] { value },
            routeSegments,
            1,
            out var routeValues
        );

        Assert.Equal(expected, matched);

        if (expected)
            Assert.Equal(value, routeValues["value"]);
        else
            Assert.Empty(routeValues);
    }

    [Fact]
    public void TryMatchRejectsUnsupportedConstraint()
    {
        // Unsupported constraints should normally be blocked by MAR008 at
        // compile time. The matcher still rejects them as a safe runtime fallback.

        var routeSegments = new[]
        {
            new MiniApiRouteSegment("{value:unsupported}", "value", "unsupported", false, true)
        };

        var matched = MiniApiRouteMatcher.TryMatch(
            new[] { "anything" },
            routeSegments,
            1,
            out var routeValues
        );

        Assert.False(matched);
        Assert.Empty(routeValues);
    }

    [Fact]
    public void TryMatchRejectsDecodedValueThatDoesNotSatisfyConstraint()
    {
        var routeSegments = new[]
        {
            new MiniApiRouteSegment("{widgetId:int}", "widgetId", "int", false, true)
        };

        var matched = MiniApiRouteMatcher.TryMatch(
            new[] { "Caf%C3%A9" },
            routeSegments,
            1,
            out var routeValues
        );

        Assert.False(matched);
        Assert.Empty(routeValues);
    }

    [Fact]
    public void TryMatchMatchesStaticSegmentsCaseInsensitively()
    {
        var routeSegments = new[]
        {
            new MiniApiRouteSegment("health", "health", null, false, false)
        };

        var matched = MiniApiRouteMatcher.TryMatch(
            new[] { "HEALTH" },
            routeSegments,
            1,
            out var routeValues
        );

        Assert.True(matched);
        Assert.Empty(routeValues);
    }

    [Fact]
    public void TryMatchRejectsDifferentStaticSegment()
    {
        // Static route segments are case-insensitive but must otherwise match
        // exactly. A different literal must not fall through as a route match.

        var routeSegments = new[]
        {
            new MiniApiRouteSegment("health", "health", null, false, false)
        };

        var matched = MiniApiRouteMatcher.TryMatch(
            new[] { "widgets" },
            routeSegments,
            1,
            out var routeValues
        );

        Assert.False(matched);
        Assert.Empty(routeValues);
    }

    [Fact]
    public void TryMatchCapturesMultipleRouteParameters()
    {
        var routeSegments = new[]
        {
            new MiniApiRouteSegment("{category}", "category", null, false, true),
            new MiniApiRouteSegment("{widgetId:int}", "widgetId", "int", false, true)
        };

        var matched = MiniApiRouteMatcher.TryMatch(
            new[] { "Caf%C3%A9", "42" },
            routeSegments,
            2,
            out var routeValues
        );

        Assert.True(matched);
        Assert.Equal("Café", routeValues["category"]);
        Assert.Equal("42", routeValues["widgetId"]);
    }

    [Fact]
    public void TryMatchAllowsMissingTrailingOptionalSegment()
    {
        var routeSegments = new[]
        {
            new MiniApiRouteSegment("{category}", "category", null, false, true),
            new MiniApiRouteSegment("{widgetName}", "widgetName", null, false, true)
        };

        var matched = MiniApiRouteMatcher.TryMatch(
            new[] { "tools" },
            routeSegments,
            1,
            out var routeValues
        );

        Assert.True(matched);
        Assert.Equal("tools", routeValues["category"]);
        Assert.False(routeValues.ContainsKey("widgetName"));
    }

    [Fact]
    public void TryMatchRejectsMissingRequiredSegment()
    {
        var routeSegments = new[]
        {
            new MiniApiRouteSegment("{category}", "category", null, false, true),
            new MiniApiRouteSegment("{widgetId:int}", "widgetId", "int", false, true)
        };

        var matched = MiniApiRouteMatcher.TryMatch(
            new[] { "tools" },
            routeSegments,
            2,
            out _
        );

        Assert.False(matched);
    }

    [Fact]
    public void TryMatchRejectsAdditionalSegmentsWithoutCatchAll()
    {
        var routeSegments = new[]
        {
            new MiniApiRouteSegment("{widgetName}", "widgetName", null, false, true)
        };

        var matched = MiniApiRouteMatcher.TryMatch(
            new[] { "first", "second" },
            routeSegments,
            1,
            out var routeValues
        );

        Assert.False(matched);
        Assert.Empty(routeValues);
    }

    [Fact]
    public void TryMatchUnconstrainedRouteParameterAlwaysMatches()
    {
        // A missing constraint means the segment is unrestricted. This test
        // protects the null-constraint behavior shared by ordinary parameters
        // and catch-all parameters.

        var routeSegments = new[]
        {
            new MiniApiRouteSegment("{value}", "value", null, false, true)
        };

        var matched = MiniApiRouteMatcher.TryMatch(
            new[] { "anything" },
            routeSegments,
            1,
            out var routeValues
        );

        Assert.True(matched);
        Assert.Equal("anything", routeValues["value"]);
    }

    [Fact]
    public void TryMatchMatchesExplicitStringConstraint()
    {
        // The explicit string constraint is an ergonomic alias for an
        // unconstrained route parameter and still applies route decoding.

        var routeSegments = new[]
        {
            new MiniApiRouteSegment("{name:string}", "name", "string", false, true)
        };

        var matched = MiniApiRouteMatcher.TryMatch(
            new[] { "Caf%C3%A9" },
            routeSegments,
            1,
            out var routeValues
        );

        Assert.True(matched);
        Assert.Equal("Café", routeValues["name"]);
    }
}
