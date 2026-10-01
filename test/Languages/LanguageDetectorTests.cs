using TreeSitter.CodeGraph.Domain.Languages;
using TreeSitter.CodeGraph.Domain.TreeSitter;
using TreeSitter.CodeGraph.Languages;

namespace TreeSitter.CodeGraph.Tests.Languages;

public class LanguageDetectorTests
{
    private readonly LanguageDetector _sut = new();

    [Fact]
    public void DetectFromFiles_ReturnsMajorityLanguage()
    {
        var files = new[]
        {
            "src/Controllers/UserController.cs",
            "src/Services/UserService.cs",
            "src/Models/User.cs",
            "scripts/deploy.js"
        };

        var result = _sut.DetectFromFiles(files);

        Assert.True(result.HasMatch);
        Assert.Equal(AnalysisLanguage.CSharp, result.PrimaryLanguage);
        Assert.Equal(4, result.TotalMatchedFiles);
        Assert.Equal(3, result.LanguageCounts[AnalysisLanguage.CSharp]);
        Assert.Equal(1, result.LanguageCounts[AnalysisLanguage.JavaScript]);
    }

    [Fact]
    public void DetectFromFiles_EmptyList_ReturnsDefaultFallback()
    {
        var result = _sut.DetectFromFiles(
            Array.Empty<string>(), 
            new LanguageDetectionOptions { DefaultFallback = AnalysisLanguage.Php });

        Assert.False(result.HasMatch);
        Assert.Equal(AnalysisLanguage.Php, result.PrimaryLanguage);
        Assert.Equal(0, result.TotalMatchedFiles);
    }

    [Fact]
    public void DetectFromFiles_UnknownExtensions_ReturnsDefaultFallback()
    {
        var files = new[] { "README.md", "image.png", "data.json" };

        var result = _sut.DetectFromFiles(
            files, 
            new LanguageDetectionOptions { DefaultFallback = AnalysisLanguage.CSharp });

        Assert.False(result.HasMatch);
        Assert.Equal(AnalysisLanguage.CSharp, result.PrimaryLanguage);
        Assert.Equal(0, result.TotalMatchedFiles);
        Assert.Equal(3, result.TotalFilesScanned);
    }

    [Fact]
    public void Detect_FromFixtures_CSharpFolder_DetectsCSharp()
    {
        var fixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "CSharp");

        var result = _sut.Detect(fixtureDir);

        Assert.True(result.HasMatch);
        Assert.Equal(AnalysisLanguage.CSharp, result.PrimaryLanguage);
        Assert.True(result.LanguageCounts[AnalysisLanguage.CSharp] > 0);
    }

    [Fact]
    public void Detect_FromFixtures_CppFolder_DetectsCpp()
    {
        var fixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Cpp");

        var result = _sut.Detect(fixtureDir);

        Assert.True(result.HasMatch);
        Assert.Equal(AnalysisLanguage.Cpp, result.PrimaryLanguage);
        Assert.True(result.LanguageCounts[AnalysisLanguage.Cpp] > 0);
    }

    [Fact]
    public void Detect_FromFixtures_PhpFolder_DetectsPhp()
    {
        var fixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Php");

        var result = _sut.Detect(fixtureDir);

        Assert.True(result.HasMatch);
        Assert.Equal(AnalysisLanguage.Php, result.PrimaryLanguage);
        Assert.True(result.LanguageCounts[AnalysisLanguage.Php] > 0);
    }

    [Fact]
    public void Detect_FromFixtures_JavaScriptFolder_DetectsJavaScript()
    {
        var fixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "JavaScript");

        var result = _sut.Detect(fixtureDir);

        Assert.True(result.HasMatch);
        Assert.Equal(AnalysisLanguage.JavaScript, result.PrimaryLanguage);
        Assert.True(result.LanguageCounts[AnalysisLanguage.JavaScript] > 0);
    }

    [Fact]
    public void Detect_WithExcludedFolders_IgnoresSpecifiedFolders()
    {
        var fixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "CSharp");
        var options = new LanguageDetectionOptions
        {
            // Exclude "Models", "Services", "Controllers", "Helpers"
            ExcludedFolders = new[] { "Controllers", "Helpers", "Models", "Services" },
            DefaultFallback = null
        };

        var result = _sut.Detect(fixtureDir, options);

        // When all subfolders with files are excluded, no files should match
        Assert.False(result.HasMatch);
        Assert.Null(result.PrimaryLanguage);
    }

    [Fact]
    public void Detect_NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        Assert.Throws<DirectoryNotFoundException>(() => _sut.Detect("invalid/path/that/does/not/exist"));
    }

    [Fact]
    public async Task DetectAsync_ReturnsIdenticalResultToSync()
    {
        var fixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "CSharp");

        var syncResult = _sut.Detect(fixtureDir);
        var asyncResult = await _sut.DetectAsync(fixtureDir);

        Assert.Equal(syncResult.PrimaryLanguage, asyncResult.PrimaryLanguage);
        Assert.Equal(syncResult.TotalMatchedFiles, asyncResult.TotalMatchedFiles);
    }
}

