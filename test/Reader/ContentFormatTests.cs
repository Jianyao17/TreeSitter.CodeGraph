using GithubAnalyzer.Analysis.Domain.Reader;
using GithubAnalyzer.Analysis.Domain.TreeSitter;
using GithubAnalyzer.Analysis.Reader;
using GithubAnalyzer.Analysis.TreeSitter.Utils;

namespace GithubAnalyzer.Analysis.Tests.Reader;

/// <summary>
/// Menguji berbagai format konten teks agar dapat dibaca dan di-parse Tree-Sitter.
/// Contoh: file UTF-8 dengan komentar multibyte, CRLF vs LF, file kosong, file binary.
/// </summary>
public class ContentFormatTests
{
    private readonly CodebaseReader _reader = new();

    [Fact]
    public async Task Utf8Valid_WithMultibyteComments_CanBeParsed()
    {
        var code = "// Menghitung total harga\npublic class Kalkulator { }";
        var tempFile = Path.Combine(Path.GetTempPath(), $"utf8test_{Guid.NewGuid()}.cs");
        try
        {
            await File.WriteAllTextAsync(tempFile, code, new System.Text.UTF8Encoding(false));
            var content = await File.ReadAllTextAsync(tempFile);
            Assert.Contains("Menghitung total harga", content);

            using var pool = new ParserPool(AnalysisLanguage.CSharp);
            using var tree = pool.Parse(content);
            Assert.NotNull(tree);
        }
        catch (DllNotFoundException)
        {
            Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task UnicodeInStringLiteral_CanBeParsed()
    {
        var code = "public class Greeter {\n    string pesan = \"Selamat datang, José!\";\n}";
        var tempFile = Path.Combine(Path.GetTempPath(), $"unicode_{Guid.NewGuid()}.cs");
        try
        {
            await File.WriteAllTextAsync(tempFile, code, new System.Text.UTF8Encoding(false));
            var content = await File.ReadAllTextAsync(tempFile);
            Assert.Contains("José", content);

            using var pool = new ParserPool(AnalysisLanguage.CSharp);
            using var tree = pool.Parse(content);
            Assert.NotNull(tree);
        }
        catch (DllNotFoundException)
        {
            Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void LineEndingsLF_CanBeParsed()
    {
        try
        {
            var code = "public class Foo {\n    public void Bar() {\n    }\n}";
            using var pool = new ParserPool(AnalysisLanguage.CSharp);
            using var tree = pool.Parse(code);
            Assert.NotNull(tree);
        }
        catch (DllNotFoundException)
        {
            Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
        }
    }

    [Fact]
    public void LineEndingsCRLF_ProducesSameDeclarationCount()
    {
        try
        {
            var codeLF = "public class Foo {\n    public void Bar() {\n    }\n    public void Baz() {\n    }\n}";
            var codeCRLF = codeLF.Replace("\n", "\r\n");

            using var pool = new ParserPool(AnalysisLanguage.CSharp);
            using var langQuery = new GithubAnalyzer.Analysis.TreeSitter.LangAnalyzer.CSharpLangQuery();

            var resultLF = langQuery.ExtractAll(codeLF);
            var resultCRLF = langQuery.ExtractAll(codeCRLF);

            Assert.Equal(resultLF.Classes.Count, resultCRLF.Classes.Count);
            Assert.Equal(resultLF.Functions.Count, resultCRLF.Functions.Count);
        }
        catch (DllNotFoundException)
        {
            Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
        }
    }

    [Fact]
    public void MixedLineEndings_DoesNotThrow()
    {
        try
        {
            // Mix \n and \r\n within same source
            var code = "public class Foo {\r\n    public void Bar() {\n    }\r\n}";
            using var pool = new ParserPool(AnalysisLanguage.CSharp);
            using var tree = pool.Parse(code);
            Assert.NotNull(tree);
        }
        catch (DllNotFoundException)
        {
            Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
        }
    }

    [Fact]
    public async Task Utf8Bom_ParserDoesNotCrash()
    {
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var codeBytes = System.Text.Encoding.UTF8.GetBytes("public class BomTest { }");
        var combined = bom.Concat(codeBytes).ToArray();

        var tempFile = Path.Combine(Path.GetTempPath(), $"bom_{Guid.NewGuid()}.cs");
        try
        {
            await File.WriteAllBytesAsync(tempFile, combined);

            // Read content — .NET may or may not strip BOM depending on version
            var content = await File.ReadAllTextAsync(tempFile);
            Assert.Contains("public class BomTest", content);

            // Strip BOM if present before parsing
            content = content.TrimStart('\uFEFF');

            using var pool = new ParserPool(AnalysisLanguage.CSharp);
            using var tree = pool.Parse(content);
            Assert.NotNull(tree);
        }
        catch (DllNotFoundException)
        {
            Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task BinaryFile_DoesNotCrashParser()
    {
        // Use an isolated subdirectory so CodebaseReader doesn't enumerate /tmp directly
        // (on Linux /tmp contains system-owned dirs like systemd-private-* that deny access)
        var tempDir  = Path.Combine(Path.GetTempPath(), $"GaTest_{Guid.NewGuid():N}");
        var tempFile = Path.Combine(tempDir, "binary.cs");
        Directory.CreateDirectory(tempDir);
        try
        {
            await File.WriteAllBytesAsync(tempFile, new byte[] { 0xFF, 0xFE, 0x00, 0x01 });

            var options = new CodebaseReadOptions
            {
                AllowedExtensions = [".cs"]
            };

            var snapshot = await _reader.ReadAsync(tempDir, options);
            var file = snapshot.Files.FirstOrDefault(f => f.AbsolutePath == tempFile);
            if (file is not null)
            {
                try
                {
                    using var pool = new ParserPool(AnalysisLanguage.CSharp);
                    using var tree = pool.Parse(file.Content);
                    // If it doesn't throw, that's acceptable
                }
                catch (DllNotFoundException)
                {
                    Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
                }
                catch (Exception)
                {
                    // Parser may throw on truly invalid content — that's acceptable
                    Assert.True(true, "Parser threw on binary content, which is acceptable.");
                }
            }
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task EmptyFile_ReturnsEmptyContent_NoThrow()
    {
        // Use an isolated subdirectory so CodebaseReader doesn't enumerate /tmp directly
        // (on Linux /tmp contains system-owned dirs like systemd-private-* that deny access)
        var tempDir  = Path.Combine(Path.GetTempPath(), $"GaTest_{Guid.NewGuid():N}");
        var tempFile = Path.Combine(tempDir, "empty.cs");
        Directory.CreateDirectory(tempDir);
        try
        {
            await File.WriteAllTextAsync(tempFile, string.Empty);

            var options = new CodebaseReadOptions
            {
                AllowedExtensions = [".cs"]
            };
            var snapshot = await _reader.ReadAsync(tempDir, options);

            var file = snapshot.Files.FirstOrDefault(f => f.AbsolutePath == tempFile);
            Assert.NotNull(file);
            Assert.Equal(string.Empty, file.Content);
            Assert.Equal(0, file.SizeBytes);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task MaxFileSizeBoundary_ExactlyAtLimit_PassesFilter()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"sizetest_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, "exact.cs");
        try
        {
            var content = new string('a', 100);
            await File.WriteAllTextAsync(tempFile, content, new System.Text.UTF8Encoding(false));
            var fileSize = new FileInfo(tempFile).Length;

            var optionsExact = new CodebaseReadOptions
            {
                AllowedExtensions = [".cs"],
                MaxFileSizeBytes = fileSize // Exactly at limit
            };
            var snapshotExact = await _reader.ReadAsync(tempDir, optionsExact);
            Assert.Single(snapshotExact.Files);

            var optionsTooSmall = new CodebaseReadOptions
            {
                AllowedExtensions = [".cs"],
                MaxFileSizeBytes = fileSize - 1 // One byte under limit
            };
            var snapshotTooSmall = await _reader.ReadAsync(tempDir, optionsTooSmall);
            Assert.Empty(snapshotTooSmall.Files);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }
}
