using TreeSitter.CodeGraph.Domain.Graph;
using TreeSitter.CodeGraph.Domain.Reader;
using TreeSitter.CodeGraph.Domain.TreeSitter;

namespace TreeSitter.CodeGraph.Interfaces;

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
    IAsyncEnumerable<TreeSitterProgress<Domain.Graph.CodeGraph>> AnalyzeAsync(
        CodebaseSnapshot snapshot,
        AnalysisLanguage language,
        CancellationToken cancellationToken = default);
}


