using System.Text.RegularExpressions;
using Xunit;

namespace OpenJobEngine.Api.Tests;

public sealed class ReleaseGovernanceTests
{
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..",
        "..",
        "..",
        "..",
        ".."));

    [Fact]
    public void ReleaseGovernance_DocsAndExportScriptsArePublished()
    {
        var readme = File.ReadAllText(Path.Combine(RepoRoot, "README.md"));
        var versioning = File.ReadAllText(Path.Combine(RepoRoot, "docs", "versioning.md"));
        var releaseNotes = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        var apiCompatibility = File.ReadAllText(Path.Combine(RepoRoot, "docs", "api-compatibility.md"));
        var deployment = File.ReadAllText(Path.Combine(RepoRoot, "docs", "deployment-and-support.md"));

        Assert.Contains("docs/release-notes.md", readme);
        Assert.Contains("docs/api-compatibility.md", readme);
        Assert.Contains("docs/deployment-and-support.md", readme);
        Assert.Contains("export release notes", versioning, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("export-release-notes.ps1", releaseNotes, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("export-release-notes.sh", releaseNotes, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Breaking changes", apiCompatibility);
        Assert.Contains("SQLite", deployment, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PostgreSQL", deployment, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Stable core API contract", deployment, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("support expectations", deployment, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Changelog_ReleaseSection_IsExportableForCurrentBaseline()
    {
        var changelog = File.ReadAllText(Path.Combine(RepoRoot, "CHANGELOG.md"));
        var match = Regex.Match(
            changelog,
            @"(?ms)^## \[0\.4\.0\].*?(?=^## \[|\z)");

        Assert.True(match.Success);

        var section = match.Value;
        Assert.StartsWith("## [0.4.0]", section);
        Assert.Contains("### Added", section);
        Assert.Contains("### Fixed", section);
    }
}
