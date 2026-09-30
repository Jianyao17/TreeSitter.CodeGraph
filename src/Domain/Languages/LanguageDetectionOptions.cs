using GithubAnalyzer.Analysis.Domain.TreeSitter;

namespace GithubAnalyzer.Analysis.Domain.Languages;

/// <summary>
/// Opsi konfigurasi untuk proses deteksi bahasa.
/// </summary>
public sealed class LanguageDetectionOptions
{
    /// <summary>
    /// Daftar nama folder yang diabaikan saat scanning filesystem.
    /// Contoh: "node_modules", "bin", "obj", "vendor".
    /// </summary>
    public IReadOnlyCollection<string> ExcludedFolders { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Bahasa default yang dipilih jika tidak ditemukan file bahasa yang didukung.
    /// Default: CSharp (dapat di-set null jika ingin PrimaryLanguage = null jika tidak ada file yang cocok).
    /// </summary>
    public AnalysisLanguage? DefaultFallback { get; init; } = AnalysisLanguage.CSharp;
}
