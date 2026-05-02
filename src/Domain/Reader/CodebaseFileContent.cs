namespace GithubAnalyzer.Analysis.Domain.Reader;

/// <summary>
/// Konten file yang sudah dibaca dari codebase.
/// </summary>
public record CodebaseFileContent
{
    /// <summary>
    /// Path relatif terhadap root.
    /// </summary>
    public string RelativePath { get; init; } = string.Empty;

    /// <summary>
    /// Path absolut file.
    /// </summary>
    public string AbsolutePath { get; init; } = string.Empty;

    /// <summary>
    /// Ekstensi file.
    /// </summary>
    public string Extension { get; init; } = string.Empty;

    /// <summary>
    /// Ukuran file dalam byte.
    /// </summary>
    public long SizeBytes { get; init; }

    /// <summary>
    /// Isi file dalam teks.
    /// </summary>
    public string Content { get; init; } = string.Empty;
}
