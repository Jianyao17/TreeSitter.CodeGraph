using GithubAnalyzer.Analysis.Domain.Graph;
using GithubAnalyzer.Analysis.Domain.Reader;
using GithubAnalyzer.Analysis.Domain.TreeSitter;

namespace GithubAnalyzer.Analysis.Interface;

/// <summary>
/// Kontrak untuk analisis kode sumber menggunakan tree-sitter.
/// Mengembalikan progress streaming via IAsyncEnumerable.
/// </summary>
public interface ICodeAnalyzer
{
    /// <summary>
    /// Menjalankan analisis dua-fase (Declaration Mapping + Usage Scanning)
    /// pada snapshot codebase dan menghasilkan CodeGraph.
    /// </summary>
    IAsyncEnumerable<TreeSitterProgress<CodeGraph>> AnalyzeAsync(
        CodebaseSnapshot snapshot,
        AnalysisLanguage language,
        CancellationToken cancellationToken = default);
}