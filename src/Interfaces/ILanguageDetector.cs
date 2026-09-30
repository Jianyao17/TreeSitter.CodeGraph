using GithubAnalyzer.Analysis.Domain.Languages;

namespace GithubAnalyzer.Analysis.Interfaces;

/// <summary>
/// Kontrak untuk mendeteksi bahasa pemrograman dominan dalam codebase.
/// </summary>
public interface ILanguageDetector
{
    /// <summary>
    /// Mendeteksi bahasa pemrograman dominan dalam direktori secara sinkron.
    /// </summary>
    LanguageDetectionResult Detect(string directoryPath, LanguageDetectionOptions? options = null);

    /// <summary>
    /// Mendeteksi bahasa pemrograman dominan dalam direktori secara asinkron.
    /// </summary>
    Task<LanguageDetectionResult> DetectAsync(
        string directoryPath,
        LanguageDetectionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mendeteksi bahasa pemrograman dominan dari kumpulan path file yang sudah diketahui.
    /// </summary>
    LanguageDetectionResult DetectFromFiles(IEnumerable<string> filePaths, LanguageDetectionOptions? options = null);
}
