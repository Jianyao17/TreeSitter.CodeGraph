using GithubAnalyzer.Analysis.Domain.Reader;

namespace GithubAnalyzer.Analysis.Interface;

/// <summary>
/// Kontrak untuk membaca konten codebase berdasarkan filter.
/// </summary>
public interface ICodebaseReader
{
    /// <summary>
    /// Membaca file dari rootPath sesuai opsi filter.
    /// </summary>
    Task<CodebaseSnapshot> ReadAsync(
        string rootPath,
        CodebaseReadOptions options,
        CancellationToken cancellationToken = default);
}
