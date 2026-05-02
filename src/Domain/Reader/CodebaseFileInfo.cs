namespace GithubAnalyzer.Analysis.Domain.Reader;

/// <summary>
/// Metadata file untuk proses filtering.
/// </summary>
public record CodebaseFileInfo
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
}
