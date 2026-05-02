namespace GithubAnalyzer.Analysis.Domain.Reader;

/// <summary>
/// Opsi filter untuk pembacaan codebase.
/// </summary>
public sealed class CodebaseReadOptions
{
    /// <summary>
    /// Daftar ekstensi file yang diizinkan.
    /// Contoh: .cs, .php
    /// </summary>
    public IReadOnlyCollection<string> AllowedExtensions { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Daftar folder yang dikecualikan.
    /// Contoh: node_modules, vendor
    /// </summary>
    public IReadOnlyCollection<string> ExcludedFolders { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Batas ukuran file maksimum dalam byte.
    /// </summary>
    public long? MaxFileSizeBytes { get; init; }

    /// <summary>
    /// Filter kustom berbasis metadata file.
    /// </summary>
    public Func<CodebaseFileInfo, bool>? CustomFilter { get; init; }
}
