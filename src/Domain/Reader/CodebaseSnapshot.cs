namespace GithubAnalyzer.Analysis.Domain.Reader;

/// <summary>
/// Hasil pembacaan codebase beserta konten file.
/// </summary>
public sealed class CodebaseSnapshot
{
    /// <summary>
    /// Root path dari codebase.
    /// </summary>
    public string RootPath { get; init; } = string.Empty;

    /// <summary>
    /// Nama repository yang bersih (tanpa author/hash).
    /// </summary>
    public string RepositoryName { get; set; } = string.Empty;

    /// <summary>
    /// Daftar file yang lolos filter.
    /// </summary>
    public List<CodebaseFileContent> Files { get; init; } = new();
}
