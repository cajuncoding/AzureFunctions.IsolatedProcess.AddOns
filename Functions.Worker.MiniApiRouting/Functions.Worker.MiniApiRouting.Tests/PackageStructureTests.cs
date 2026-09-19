using System.IO.Compression;
using Xunit;

namespace Functions.Worker.MiniApiRouting.Tests;

public sealed class PackageStructureTests
{
    [Fact]
    public void PackedPackageContainsRuntimeAndAnalyzerAssetsWhenPackageExists()
    {
        var package = FindLatestPackage();
        if (package is null)
            return;

        using var archive = ZipFile.OpenRead(package);
        var entries = archive.Entries.Select(entry => entry.FullName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("lib/net8.0/Functions.Worker.MiniApiRouting.dll", entries);
        Assert.Contains("lib/net10.0/Functions.Worker.MiniApiRouting.dll", entries);
        Assert.Contains("analyzers/dotnet/cs/Functions.Worker.MiniApiRouting.Generators.dll", entries);
        Assert.Contains("README.md", entries);
    }

    private static string? FindLatestPackage()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AzureFunctions.IsolatedProcess.AddOns.sln")))
            directory = directory.Parent;

        if (directory is null)
            return null;

        return Directory.GetFiles(directory.FullName, "Functions.Worker.MiniApiRouting.*.nupkg", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }
}
