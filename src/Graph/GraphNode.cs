namespace GithubAnalyzer.Analysis.Graph;

/// <summary>
/// Representasi node dalam graf kode.
/// Node adalah entitas utama seperti Folder, File, Class, Function.
/// </summary>
public record GraphNode
{
    /// <summary>
    /// ID unik node (disarankan: kombinasi path + nama)
    /// </summary>
    public string Id { get; init; } = default!;

    /// <summary>
    /// Nama yang ditampilkan (misal: main.php, getUser, UserService)
    /// </summary>
    public string Label { get; init; } = default!;

    /// <summary>
    /// Path absolut / relatif dari node dalam project
    /// </summary>
    public string Path { get; init; } = default!;

    /// <summary>
    /// Tipe node (Folder, File, Class, Function)
    /// </summary>
    public NodeType Type { get; init; }
}

/// <summary>
/// Jenis node dalam graf kode.
/// </summary>
public enum NodeType
{
    Folder,
    File,
    Class,
    Function
}