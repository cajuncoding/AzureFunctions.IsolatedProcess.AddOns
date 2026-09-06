using System.Text;

namespace Functions.Worker.AddOns.MiniApiRouting.Generators;

internal static class SourceBuilderExtensions
{
    internal static StringBuilder AppendLines(this StringBuilder source, string lines)
    {
        source.AppendLine(lines.Trim('\r', '\n'));
        return source;
    }

    internal static StringBuilder AppendBlankLine(this StringBuilder source)
    {
        source.AppendLine();
        return source;
    }
}
