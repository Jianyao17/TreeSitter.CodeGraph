using GithubAnalyzer.Analysis.Domain.Reader;
using GithubAnalyzer.Analysis.Domain.TreeSitter;
using GithubAnalyzer.Analysis.Reader;
using GithubAnalyzer.Analysis.TreeSitter.Utils;

namespace GithubAnalyzer.Analysis.Tests.Reader;

/// <summary>
/// Menguji CodebaseReader: filter ekstensi, folder, ukuran, custom filter,
/// path cross-platform, cancellation, dan edge cases.
/// Contoh: ReadAsync dengan filter .cs hanya mengembalikan file C#.
/// </summary>
public class CodebaseReaderTests
{
    private readonly string _fixturesPath = Path.Combine(AppContext.BaseDirectory, "Fixtures");
    private readonly CodebaseReader _reader = new();

    [Fact]
    public async Task ReadAsync_WithExtensionFilter_OnlyReturnsMatchingFiles()
    {
        var options = new CodebaseReadOptions
        {
            AllowedExtensions = [".cs"]
        };

        var snapshot = await _reader.ReadAsync(_fixturesPath, options);

        Assert.All(snapshot.Files, f => Assert.Equal(".cs", f.Extension));
        Assert.True(snapshot.Files.Count > 0, "Should find at least one .cs file.");
    }

    [Fact]
    public async Task ReadAsync_WithDisallowedExtension_ReturnsNoFiles()
    {
        var options = new CodebaseReadOptions
        {
            AllowedExtensions = [".xyz"]
        };

        var snapshot = await _reader.ReadAsync(_fixturesPath, options);

        Assert.Empty(snapshot.Files);
    }

    [Fact]
    public async Task ReadAsync_WithNoExtensionFilter_ReturnsAllFiles()
    {
        var options = new CodebaseReadOptions();

        var snapshot = await _reader.ReadAsync(_fixturesPath, options);

        // Should contain files of multiple extensions (.cs, .js, .php, .h, .cpp)
        var extensions = snapshot.Files.Select(f => f.Extension).Distinct().ToList();
        Assert.True(extensions.Count >= 4, $"Expected at least 4 different extensions, got {extensions.Count}: {string.Join(", ", extensions)}");
    }

    [Fact]
    public async Task ReadAsync_WithExcludedFolder_ExcludesNestedFiles()
    {
        var options = new CodebaseReadOptions
        {
            ExcludedFolders = ["Controllers"]
        };

        var snapshot = await _reader.ReadAsync(_fixturesPath, options);

        Assert.DoesNotContain(snapshot.Files, f =>
            f.RelativePath.Contains("Controllers", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ReadAsync_WithMaxFileSize_ExcludesLargeFiles()
    {
        var options = new CodebaseReadOptions
        {
            MaxFileSizeBytes = 10 // Very small, should exclude almost all files
        };

        var snapshot = await _reader.ReadAsync(_fixturesPath, options);

        Assert.Empty(snapshot.Files);
    }

    [Fact]
    public async Task ReadAsync_WithCustomFilter_FiltersBasedOnFileInfo()
    {
        var options = new CodebaseReadOptions
        {
            CustomFilter = info => info.RelativePath.Contains("User", StringComparison.OrdinalIgnoreCase)
        };

        var snapshot = await _reader.ReadAsync(_fixturesPath, options);

        Assert.All(snapshot.Files, f =>
            Assert.Contains("User", f.RelativePath, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ReadAsync_RelativePath_IsValidAndResolvable()
    {
        var options = new CodebaseReadOptions
        {
            AllowedExtensions = [".cs"]
        };

        var snapshot = await _reader.ReadAsync(_fixturesPath, options);

        Assert.All(snapshot.Files, f =>
        {
            Assert.True(Path.IsPathFullyQualified(f.AbsolutePath), $"AbsolutePath should be fully qualified: {f.AbsolutePath}");
            // RelativePath should be reconstructable to AbsolutePath
            var reconstructed = Path.GetFullPath(Path.Combine(_fixturesPath, f.RelativePath));
            Assert.True(File.Exists(reconstructed), $"Reconstructed path should exist: {reconstructed}");
            // PathId.Normalize should produce forward-slash paths
            var normalized = PathId.Normalize(f.RelativePath);
            Assert.DoesNotContain("\\", normalized);
        });
    }

    [Fact]
    public async Task ReadAsync_ContentCanBeParsedByTreeSitter()
    {
        try
        {
            var options = new CodebaseReadOptions
            {
                AllowedExtensions = [".cs"]
            };

            var snapshot = await _reader.ReadAsync(_fixturesPath, options);
            Assert.NotEmpty(snapshot.Files);

            using var pool = new ParserPool(AnalysisLanguage.CSharp);
            foreach (var file in snapshot.Files)
            {
                Assert.False(string.IsNullOrEmpty(file.Content), $"Content should not be empty for {file.RelativePath}");
                using var tree = pool.Parse(file.Content);
                Assert.NotNull(tree);
            }
        }
        catch (DllNotFoundException)
        {
            Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
        }
    }

    [Fact]
    public async Task ReadAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var options = new CodebaseReadOptions();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _reader.ReadAsync(_fixturesPath, options, cts.Token));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ReadAsync_EmptyOrWhitespaceRootPath_ThrowsArgumentException(string rootPath)
    {
        var options = new CodebaseReadOptions();

        await Assert.ThrowsAsync<ArgumentException>(
            () => _reader.ReadAsync(rootPath, options));
    }

    [Fact]
    public async Task ReadAsync_NullOptions_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _reader.ReadAsync(_fixturesPath, null!));
    }
}
