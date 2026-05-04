using TreeSitter;
using GithubAnalyzer.Analysis.Domain.TreeSitter;

namespace GithubAnalyzer.Analysis.TreeSitter.Utils;

/// <summary>
/// Mengelola lifecycle Language dan Parser dari tree-sitter.
/// Mapping AnalysisLanguage → TreeSitter language identifier.
/// </summary>
public sealed class ParserPool : IDisposable
{
    private readonly Language _language;
    private readonly Parser _parser;
    private bool _disposed;

    public ParserPool(AnalysisLanguage language)
    {
        var langId = MapLanguageId(language);
        _language = new Language(langId);
        _parser = new Parser(_language);
    }

    public Language Language => _language;

    /// <summary>
    /// Parse source code menjadi syntax tree.
    /// </summary>
    public Tree Parse(string sourceCode)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _parser.Parse(sourceCode)
            ?? throw new InvalidOperationException("Tree-sitter gagal mem-parse source code.");
    }

    /// <summary>
    /// Mapping enum ke tree-sitter language identifier string.
    /// </summary>
    private static string MapLanguageId(AnalysisLanguage language) => language switch
    {
        AnalysisLanguage.CSharp => "c-sharp",
        AnalysisLanguage.JavaScript => "javascript",
        AnalysisLanguage.Php => "php",
        AnalysisLanguage.Cpp => "cpp",
        _ => throw new ArgumentOutOfRangeException(nameof(language), $"Bahasa '{language}' belum didukung.")
    };

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _parser.Dispose();
        _language.Dispose();
    }
}
