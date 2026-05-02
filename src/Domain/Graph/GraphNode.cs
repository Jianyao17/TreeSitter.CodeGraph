namespace GithubAnalyzer.Analysis.Domain.Graph;

/// <summary>
/// Representasi node dalam graf kode.
/// Node adalah entitas utama seperti Folder, File, Class, Function.
/// </summary>
public record GraphNode
{
    /// <summary>
    /// GUID unik node
    /// </summary>
    public Guid Id { get; init; } = default!;

    /// <summary>
    /// Nama yang ditampilkan (misal: main.php, getUser, UserService)
    /// </summary>
    public string Label { get; init; } = default!;

    /// <summary>
    /// Path absolut / relatif dari node dalam project
    /// Contoh: 
    ///     src/Controllers/UserController.cs::Class1, 
    ///     src/Services/UserService.cs::Function1(), 
    ///     src/Models/User.cs::Class1.Function1(Param1, Param2)
    ///     Root/Namespace1/Namespace2::Class1.Function1(Param1, Param2)
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
    FolderOrNamespace,
    File,
    Class,
    Function
}