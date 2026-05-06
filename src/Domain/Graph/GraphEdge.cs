namespace GithubAnalyzer.Analysis.Domain.Graph;

/// <summary>
/// Representasi edge (relasi) antar node dalam graf kode.
/// </summary>
public record GraphEdge
{
    /// <summary>
    /// PathId node sumber
    /// </summary>
    public string From { get; init; } = default!;

    /// <summary>
    /// PathId node tujuan
    /// </summary>
    public string To { get; init; } = default!;

    /// <summary>
    /// Jenis relasi spesifik (lihat EdgeType)
    /// </summary>
    public EdgeType Type { get; init; }
}

/// <summary>
/// Jenis relasi spesifik antar node dalam graf kode.
/// BelongsTo, Define, Call, Include
/// </summary>
public enum EdgeType
{
    /// <summary>
    /// Menunjukkan hubungan kepemilikan atau hirarki.
    /// Arah: Parent → Child.
    /// Contoh: Directory → File, ParentDir → ChildDir, Namespace → File.
    /// </summary>
    BelongsTo,

    /// <summary>
    /// Menunjukkan hubungan definisi atau deklarasi.
    /// Arah: Container → Symbol.
    /// Contoh: File → Class, File → Function, Class → Function.
    /// </summary>
    Define,

    /// <summary>
    /// Menunjukkan hubungan pemanggilan atau penggunaan.
    /// Arah: Definition → Caller (sumber definisi → tempat dipanggil).
    /// Contoh: FuncA → FuncB (FuncA dipanggil oleh FuncB).
    /// </summary>
    Call,

    /// <summary>
    /// Menunjukkan hubungan inklusi atau import.
    /// Khusus untuk bahasa yang mendukung mekanisme include/require (misal: PHP, C/C++).
    /// Contoh: File → File (include/require).
    /// </summary>
    Include,
}