namespace GithubAnalyzer.Analysis.Graph;

/// <summary>
/// Representasi edge (relasi) antar node dalam graf kode.
/// </summary>
public record GraphEdge
{
    /// <summary>
    /// ID node sumber
    /// </summary>
    public string Source { get; init; } = default!;

    /// <summary>
    /// ID node tujuan
    /// </summary>
    public string Target { get; init; } = default!;

    /// <summary>
    /// Kategori relasi:
    /// - SOURCE_RELATION: relasi struktur / definisi
    /// - USE_RELATION: relasi penggunaan / dependensi
    /// </summary>
    public EdgeCategory Category { get; init; }

    /// <summary>
    /// Jenis relasi spesifik (lihat EdgeType)
    /// </summary>
    public EdgeType Type { get; init; }
}

/// <summary>
/// Kategori besar relasi dalam graf.
/// </summary>
public enum EdgeCategory
{
    /// <summary>
    /// Relasi struktur / asal definisi kode
    /// </summary>
    SourceRelation,

    /// <summary>
    /// Relasi penggunaan / dependensi kode
    /// </summary>
    UseRelation
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