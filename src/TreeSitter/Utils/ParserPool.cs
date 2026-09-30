using TreeSitter;
using GithubAnalyzer.Analysis.Domain.TreeSitter;
using GithubAnalyzer.Analysis.Languages;

namespace GithubAnalyzer.Analysis.TreeSitter.Utils;

/// <summary>
/// Mengelola lifecycle Language dan Parser dari tree-sitter.
/// Resolusi AnalysisLanguage → TreeSitter language identifier melalui LanguageRegistry.
/// </summary>
public sealed class ParserPool : IDisposable
{
    private readonly Language _language;
    private readonly Parser _parser;
    private bool _disposed;

    public ParserPool(AnalysisLanguage language)
    {
        var langId = LanguageRegistry.Default.GetTreeSitterLanguageId(language);
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

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _parser.Dispose();
        _language.Dispose();
    }
}
