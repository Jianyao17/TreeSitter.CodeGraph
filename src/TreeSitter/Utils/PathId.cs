namespace GithubAnalyzer.Analysis.TreeSitter.Utils;

/// <summary>
/// Utility untuk membangun PathId sesuai format konvensi project.
/// Format: {relative_path}::{symbol_path}.{symbol}
/// </summary>
public static class PathId
{
    /// <summary>
    /// Bangun PathId lengkap untuk class atau function.
    /// Contoh: "src/Controllers/UserController.cs::Namespace.UserController.GetUser(int)"
    /// </summary>
    public static string Build(string relativePath, string? symbolPath, string symbol)
    {
        var path = Normalize(relativePath);

        if (string.IsNullOrEmpty(symbolPath))
            return $"{path}::{symbol}";

        return $"{path}::{symbolPath}.{symbol}";
    }

    /// <summary>
    /// PathId untuk folder (tanpa symbol).
    /// Contoh: "src/Controllers::"
    /// </summary>
    public static string ForFolder(string relativePath)
    {
        return $"{Normalize(relativePath)}::";
    }

    /// <summary>
    /// PathId untuk namespace tanpa file.
    /// Contoh: "::App.Services"
    /// </summary>
    public static string ForNamespace(string namespaceName)
    {
        return $"::{namespaceName}";
    }

    /// <summary>
    /// PathId untuk file node.
    /// Contoh: "src/Controllers/UserController.cs"
    /// </summary>
    public static string ForFile(string relativePath)
    {
        return Normalize(relativePath);
    }

    /// <summary>
    /// Format nama function dengan parameter types.
    /// Contoh: "GetUser(int,string)" atau "getUser()"
    /// </summary>
    public static string FormatFunction(string name, string paramTypes)
    {
        return string.IsNullOrEmpty(paramTypes)
            ? $"{name}()"
            : $"{name}({paramTypes})";
    }

    /// <summary>
    /// Normalisasi path separator ke forward slash.
    /// </summary>
    public static string Normalize(string path)
    {
        return path.Replace('\\', '/');
    }
}
