namespace GithubAnalyzer.Analysis.Domain.Graph;

/// <summary>
/// Representasi edge (relasi) antar node dalam graf kode.
/// </summary>
public record GraphEdge
{
    /// <summary>
    /// GUID node sumber
    /// </summary>
    public Guid From { get; init; } = default!;

    /// <summary>
    /// GUID node tujuan
    /// </summary>
    public Guid To { get; init; } = default!;

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
    /// Contoh: Folder → File, Folder → Folder.
    /// </summary>
    BelongsTo,

    /// <summary>
    /// Menunjukkan hubungan definisi atau deklarasi.
    /// Contoh: File → Function, File → Class, Class → Function.
    /// </summary>
    Define,

    /// <summary>
    /// Menunjukkan hubungan pemanggilan atau penggunaan.
    /// Contoh: Function1 → Function2, Class1 → Class2, Func1 → Class1, File1 → Class1, File1 → Func1.
    /// </summary>
    Call,

    /// <summary>
    /// Menunjukkan hubungan inklusi atau import.
    /// Example: File → File (include/require).
    /// </summary>
    Include,
}