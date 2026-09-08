using System.Collections.Immutable;

namespace Functions.Worker.AddOns.MiniApiRouting;

public static class MiniApiVerbs
{
    public const string Delete = "DELETE";
    public const string Get = "GET";
    public const string Head = "HEAD";
    public const string Options = "OPTIONS";
    public const string Patch = "PATCH";
    public const string Post = "POST";
    public const string Put = "PUT";

    public static readonly ImmutableArray<string> All = ImmutableArray.Create(
        Delete,
        Get,
        Head,
        Options,
        Patch,
        Post,
        Put
    );
}
