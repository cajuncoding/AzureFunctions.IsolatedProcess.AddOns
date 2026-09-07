using System.ComponentModel;

namespace Functions.Worker.AddOns.MiniApiRouting;

[EditorBrowsable(EditorBrowsableState.Never)]
public readonly record struct MiniApiRouteSegment(
    string Template,
    string Name,
    string? Constraint,
    bool IsCatchAll,
    bool IsParameter
);