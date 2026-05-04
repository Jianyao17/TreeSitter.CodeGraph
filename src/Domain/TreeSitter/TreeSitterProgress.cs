namespace GithubAnalyzer.Analysis.Domain.TreeSitter;

/// <summary>
/// Model progress untuk streaming hasil analisis via IAsyncEnumerable.
/// Yield terakhir akan memiliki IsCompleted = true dan Result berisi data final.
/// </summary>
public sealed class TreeSitterProgress<T> where T : class
{
    /// <summary>
    /// Persentase progress (0-100).
    /// </summary>
    public int Percentage { get; init; }

    /// <summary>
    /// Pesan deskriptif tentang tahap saat ini.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Apakah proses telah selesai.
    /// </summary>
    public bool IsCompleted { get; init; }

    /// <summary>
    /// Hasil akhir, hanya terisi pada yield terakhir (IsCompleted = true).
    /// </summary>
    public T? Result { get; init; }
}
