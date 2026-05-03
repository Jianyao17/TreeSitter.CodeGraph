namespace GithubAnalyzer.Analysis.Domain.Graph;

/// <summary>
/// Representasi node dalam graf kode.
/// Node adalah entitas utama seperti Folder, File, Class, Function.
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
    /// - symbol_path   : namespace / class (opsional, bisa kosong jika file-based)
    /// - symbol        : nama class / function
    /// - function      : wajib menggunakan () atau (parameter_type)
    ///
    /// Aturan:
    /// - Jika symbol_path kosong → tidak perlu titik (.)
    /// - Gunakan format konsisten untuk parameter (misal: int, string)
    /// - Untuk folder tanpa file spesifik, gunakan format {relative_path}:: (tanda :: di akhir)
    /// - Untuk namespace tanpa class spesifik, gunakan format ::{symbol_path} (tanda :: di awal)
    ///
    /// Contoh:
    ///     src/Controllers/UserController.cs::UserController
    ///     src/Services/UserService.js::getUser()
    ///     src/Models/User.cs::User.getName(string)
    ///     src/Services/UserService.php::App.Services.UserServiceClass.getUser(int)
    ///     src/Services::    --> untuk folder tengah tanpa file spesifik
    ///     ::App.Services    --> untuk namespace tengah tanpa class spesifik
    /// </summary>
    public string PathId { get; init; } = default!;

    /// <summary>
    /// Nama yang ditampilkan (misal: main.php, getUser, UserService)
    /// </summary>
    public string Label { get; init; } = default!;

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