using TreeSitter;
using GithubAnalyzer.Analysis.Domain.TreeSitter;
using GithubAnalyzer.Analysis.TreeSitter.Utils;

namespace GithubAnalyzer.Analysis.TreeSitter;

/// <summary>
/// Abstract base class untuk query node tree-sitter per bahasa.
/// Menggunakan Template Method Pattern: logika umum di sini,
/// kelas turunan hanya menyuplai implementasi query spesifik.
/// 
/// Output: LangQueryResult yang standar dan bahasa-agnostik,
/// siap dikonsumsi oleh TreeSitterAnalyzer untuk analisa relasi.
/// </summary>
public abstract class BaseLangQuery : IDisposable
{
    private readonly ParserPool _pool;
    private bool _disposed;

    protected BaseLangQuery(AnalysisLanguage language)
    {
        _pool = new ParserPool(language);
    }

    /// <summary>
    /// Apakah bahasa ini menggunakan namespace (C#, PHP, C++)
    /// atau folder hierarchy (JavaScript).
    /// </summary>
    public abstract bool UsesNamespace { get; }

    /// <summary>
    /// Template method: parse source code dan extract semua informasi.
    /// Mengembalikan LangQueryResult yang standar.
    /// </summary>
    public LangQueryResult ExtractAll(string sourceCode)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        using var tree = _pool.Parse(sourceCode);
        var root = tree.RootNode;
        var lang = _pool.Language;

        return new LangQueryResult
        {
            Namespaces = QueryNamespaces(root, lang),
            Classes = QueryClasses(root, lang),
            Functions = QueryFunctions(root, lang),
            Calls = QueryCalls(root, lang),
            TypeRefs = QueryTypeRefs(root, lang),
            Includes = QueryIncludes(root, lang)
        };
    }

    // === Abstract query methods — wajib diimplementasi tiap bahasa ===

    protected abstract List<NamespaceInfo> QueryNamespaces(Node root, Language lang);
    protected abstract List<ClassInfo> QueryClasses(Node root, Language lang);
    protected abstract List<FunctionInfo> QueryFunctions(Node root, Language lang);
    protected abstract List<CallInfo> QueryCalls(Node root, Language lang);
    protected abstract List<TypeRefInfo> QueryTypeRefs(Node root, Language lang);

    /// <summary>
    /// Override untuk bahasa yang mendukung include/import (PHP, JS, C++).
    /// Default: empty list.
    /// </summary>
    protected virtual List<IncludeInfo> QueryIncludes(Node root, Language lang) => [];

    // === Helper methods untuk subclass ===

    /// <summary>
    /// Jalankan S-expression query dan kumpulkan semua matches.
    /// </summary>
    protected static List<QueryMatch> RunQuery(string querySource, Node root, Language lang)
    {
        using var query = new Query(lang, querySource);
        var cursor = query.Execute(root);
        return cursor.Matches.ToList();
    }

    /// <summary>
    /// Jalankan query dan kembalikan captures saja (lebih ringkas untuk query sederhana).
    /// </summary>
    protected static List<QueryCapture> RunQueryCaptures(string querySource, Node root, Language lang)
    {
        using var query = new Query(lang, querySource);
        var cursor = query.Execute(root);
        return cursor.Captures.ToList();
    }

    /// <summary>
    /// Cari capture dengan nama tertentu dalam match.
    /// </summary>
    protected static string? GetCapture(QueryMatch match, string captureName)
    {
        return match.Captures
            .FirstOrDefault(c => c.Name == captureName)?.Node.Text;
    }

    /// <summary>
    /// Cari node capture untuk mendapatkan posisi baris.
    /// </summary>
    protected static Node? GetCaptureNode(QueryMatch match, string captureName)
    {
        return match.Captures
            .FirstOrDefault(c => c.Name == captureName)?.Node;
    }

    /// <summary>
    /// Tentukan parent namespace dari posisi node dalam source code.
    /// Memeriksa apakah node berada di dalam range baris namespace tertentu.
    /// </summary>
    protected static string? FindParentNamespace(int nodeLine, List<NamespaceInfo> namespaces)
    {
        // Cari namespace terdalam yang mengandung baris ini
        return namespaces
            .Where(ns => nodeLine >= ns.StartLine && nodeLine <= ns.EndLine)
            .OrderByDescending(ns => ns.StartLine) // ambil yang terdalam
            .Select(ns => ns.Name)
            .FirstOrDefault();
    }

    /// <summary>
    /// Tentukan parent class dari posisi node dalam source code.
    /// </summary>
    protected static string? FindParentClass(int nodeLine, List<ClassInfo> classes)
    {
        return classes
            .Where(c => nodeLine >= c.StartLine && nodeLine <= c.EndLine)
            .OrderByDescending(c => c.StartLine)
            .Select(c => c.Name)
            .FirstOrDefault();
    }

    /// <summary>
    /// Extract parameter types dari parameter list node.
    /// Digunakan oleh bahasa yang punya type annotation (C#, PHP, C++).
    /// </summary>
    protected static string ExtractParamTypes(string paramQuerySource, Node paramsNode, Language lang)
    {
        var types = new List<string>();

        try
        {
            using var query = new Query(lang, paramQuerySource);
            var cursor = query.Execute(paramsNode);
            foreach (var match in cursor.Matches)
            {
                var typeText = GetCapture(match, "param_type");
                if (!string.IsNullOrEmpty(typeText))
                    types.Add(typeText.Trim());
            }
        }
        catch
        {
            // Jika query gagal (misal node kosong), return empty
        }

        return string.Join(",", types);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _pool.Dispose();
        GC.SuppressFinalize(this);
    }
}
