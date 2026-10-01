using TreeSitter.CodeGraph.Domain.TreeSitter;

namespace TreeSitter.CodeGraph.Domain.Languages;

/// <summary>
/// Hasil analisis deteksi bahasa pada codebase.
/// Menyediakan bahasa dominan beserta statistik file per bahasa.
/// </summary>
public sealed record LanguageDetectionResult
{
    /// <summary>
    /// Bahasa dominan yang terdeteksi, atau fallback/null jika tidak ada file yang cocok.
    /// </summary>
    public AnalysisLanguage? PrimaryLanguage { get; init; }

    /// <summary>
    /// Total file sumber kode yang cocok dengan salah satu bahasa terdaftar.
    /// </summary>
    public int TotalMatchedFiles { get; init; }

    /// <summary>
    /// Total file yang dipindai secara keseluruhan (termasuk yang tidak cocok).
    /// </summary>
    public int TotalFilesScanned { get; init; }

    /// <summary>
    /// Rincian jumlah file per bahasa yang didukung.
    /// </summary>
    public IReadOnlyDictionary<AnalysisLanguage, int> LanguageCounts { get; init; } 
        = new Dictionary<AnalysisLanguage, int>();

    /// <summary>
    /// Menunjukkan apakah ditemukan file yang cocok dengan salah satu bahasa terdaftar.
    /// </summary>
    public bool HasMatch => PrimaryLanguage.HasValue && TotalMatchedFiles > 0;
}

