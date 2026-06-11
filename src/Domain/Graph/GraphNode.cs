namespace GithubAnalyzer.Analysis.Domain.Graph;

/// <summary>
/// Representasi node dalam graf kode.
/// Node adalah entitas utama seperti Directory, Namespace, File, Class, Function.
/// </summary>
public sealed record GraphNode
{
    /// <summary>
    /// Path Id relatif unik untuk identifikasi node dalam project.
    ///
    /// Template:
    ///     {relative_path}::{symbol_path}.{symbol}
    ///
    /// Keterangan:
    /// - relative_path : path file relatif (gunakan '/' sebagai separator)
    /// - symbol_path   : namespace / class (opsional, bisa kosong jika directory-based)
    /// - symbol        : nama class / function
    /// - function      : wajib menggunakan () atau (parameter_type)
    ///
    /// Aturan:
    /// - Jika symbol_path kosong → tidak perlu titik (.)
    /// - Gunakan format konsisten untuk parameter (misal: int, string)
    /// - Untuk directory, gunakan format {relative_path}:: (tanda :: di akhir)
    /// - Untuk file, gunakan format {relative_path}:: (tanda :: di akhir, sama seperti directory)
    /// - Untuk namespace, gunakan format ::{symbol_path} (tanda :: di awal)
    ///
    /// Contoh:
    ///     src/Controllers/UserController.cs::UserController
    ///     src/Services/UserService.js::getUser()
    ///     src/Models/User.cs::User.getName(string)
    ///     src/Services/UserService.php::App.Services.UserServiceClass.getUser(int)
    ///     src/Services::          --> untuk directory
    ///     src/Services/User.cs::  --> untuk file
    ///     ::App.Services          --> untuk namespace
    /// </summary>
    public string PathId { get; init; } = default!;

    /// <summary>
    /// Nama yang ditampilkan (misal: main.php, getUser, UserService)
    /// </summary>
    public string Label { get; init; } = default!;

    /// <summary>
    /// Tipe node (Directory, Namespace, File, Class, Function)
    /// </summary>
    public NodeType Type { get; init; }

    /// <summary>
    /// Baris awal node dalam file sumber (1-indexed). Null jika tidak berlaku (misal Directory).
    /// </summary>
    public int? StartLine { get; init; }

    /// <summary>
    /// Baris akhir node dalam file sumber (1-indexed). Null jika tidak berlaku.
    /// </summary>
    public int? EndLine { get; init; }
}

/// <summary>
/// Jenis node dalam graf kode.
/// </summary>
public enum NodeType
{
    /// <summary>
    /// Folder/direktori fisik dalam filesystem.
    /// </summary>
    Directory,

    /// <summary>
    /// Namespace/package/module logical grouping.
    /// </summary>
    Namespace,

    /// <summary>
    /// File sumber kode.
    /// </summary>
    File,

    /// <summary>
    /// Class, interface, struct, record, trait.
    /// </summary>
    Class,

    /// <summary>
    /// Function, method, constructor.
    /// </summary>
    Function
}