using System.Runtime.CompilerServices;
using GithubAnalyzer.Analysis.Domain.Graph;
using GithubAnalyzer.Analysis.Domain.Reader;
using GithubAnalyzer.Analysis.Domain.TreeSitter;
using GithubAnalyzer.Analysis.Interface;
using GithubAnalyzer.Analysis.TreeSitter.LangAnalyzer;
using GithubAnalyzer.Analysis.TreeSitter.Utils;

namespace GithubAnalyzer.Analysis.TreeSitter;

/// <summary>
/// Analyzer utama yang mengimplementasi ICodeAnalyzer.
/// Menggunakan BaseLangQuery untuk extract data per bahasa,
/// kemudian menjalankan analisa relasi dua-fase:
///   Pass 1: Declaration Mapping (nodes + SourceRelEdges)
///   Pass 2: Usage Scanning (UseRelEdges)
/// 
/// Mendukung background queue via CancellationToken dan IAsyncEnumerable streaming.
/// </summary>
public sealed class TreeSitterAnalyzer : ICodeAnalyzer, IDisposable
{

    public async IAsyncEnumerable<TreeSitterProgress<CodeGraph>> AnalyzeAsync(
        CodebaseSnapshot snapshot, 
        AnalysisLanguage language,
        [EnumeratorCancellation] 
        CancellationToken cancelToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        using var langQuery = CreateLangQuery(language);
        var graph = new CodeGraph();
        var totalFiles = snapshot.Files.Count;

        if (totalFiles == 0)
        {
            yield return new TreeSitterProgress<CodeGraph>
            {
                Percentage = 100,
                Message = "Tidak ada file untuk dianalisis.",
                IsCompleted = true,
                Result = graph
            };
            yield break;
        }

        // === Data structure untuk menampung hasil Pass 1 ===

        // Semua declarations yang ditemukan, indexed by name untuk lookup di Pass 2
        var declaredFunctions = new List<DeclaredSymbol>();
        var declaredClasses = new List<DeclaredSymbol>();

        // Mapping file → extracted data untuk reuse di Pass 2
        var fileResults = new Dictionary<string, LangQueryResult>();

        // Set untuk tracking folder/namespace nodes yang sudah dibuat
        var createdFolderNodes = new HashSet<string>();
        var createdNamespaceNodes = new HashSet<string>();

        // ================================================================
        // PASS 1: Declaration Mapping (0% - 60%)
        // ================================================================

        yield return new TreeSitterProgress<CodeGraph>
        {
            Percentage = 0,
            Message = "Memulai Pass 1: Declaration Mapping..."
        };

        for (int i = 0; i < totalFiles; i++)
        {
            cancelToken.ThrowIfCancellationRequested();

            var file = snapshot.Files[i];
            var relativePath = PathId.Normalize(file.RelativePath);

            // Extract semua deklarasi dan usage dari file ini
            LangQueryResult result;
            try
            {
                result = langQuery.ExtractAll(file.Content);
            }
            catch (Exception)
            {
                // Skip file yang gagal di-parse (binary, corrupt, etc.)
                continue;
            }

            fileResults[relativePath] = result;

            // --- Bangun folder/namespace hierarchy ---
            BuildFolderHierarchy(relativePath, langQuery.UsesNamespace, result, graph,
                                 createdFolderNodes, createdNamespaceNodes);

            // --- File node ---
            var filePathId = PathId.ForFile(relativePath);
            graph.Nodes.Add(new GraphNode
            {
                PathId = filePathId,
                Label = Path.GetFileName(relativePath),
                Type = NodeType.File
            });

            // Edge: parent folder/namespace → file (BelongsTo)
            var parentFolderPath = GetParentFolder(relativePath);
            if (!string.IsNullOrEmpty(parentFolderPath))
            {
                var parentId = langQuery.UsesNamespace && result.Namespaces.Count > 0
                    ? PathId.ForNamespace(result.Namespaces[0].Name)
                    : PathId.ForFolder(parentFolderPath);

                graph.SourceRelEdges.Add(new GraphEdge
                {
                    From = parentId,
                    To = filePathId,
                    Type = EdgeType.BelongsTo
                });
            }

            // --- Class nodes ---
            foreach (var cls in result.Classes)
            {
                var symbolPath = cls.ParentNamespace;
                var classPathId = PathId.Build(relativePath, symbolPath, cls.Name);

                graph.Nodes.Add(new GraphNode
                {
                    PathId = classPathId,
                    Label = cls.Name,
                    Type = NodeType.Class
                });

                // Edge: File → Class (Define)
                graph.SourceRelEdges.Add(new GraphEdge
                {
                    From = filePathId,
                    To = classPathId,
                    Type = EdgeType.Define
                });

                declaredClasses.Add(new DeclaredSymbol(cls.Name, classPathId, relativePath, cls.ParentNamespace));
            }

            // --- Function nodes ---
            foreach (var func in result.Functions)
            {
                var funcLabel = PathId.FormatFunction(func.Name, func.Params);
                var symbolPath = BuildSymbolPath(func.ParentNamespace, func.ParentClass);
                var funcPathId = PathId.Build(relativePath, symbolPath, funcLabel);

                graph.Nodes.Add(new GraphNode
                {
                    PathId = funcPathId,
                    Label = funcLabel,
                    Type = NodeType.Function
                });

                // Edge: parent → function (Define)
                // Jika ada parent class, edge dari class; jika tidak, dari file
                string parentId;
                if (!string.IsNullOrEmpty(func.ParentClass))
                {
                    var classSymbolPath = func.ParentNamespace;
                    parentId = PathId.Build(relativePath, classSymbolPath, func.ParentClass);
                }
                else
                {
                    parentId = filePathId;
                }

                graph.SourceRelEdges.Add(new GraphEdge
                {
                    From = parentId,
                    To = funcPathId,
                    Type = EdgeType.Define
                });

                declaredFunctions.Add(new DeclaredSymbol(
                    func.Name, funcPathId, relativePath, func.ParentNamespace, func.ParentClass));
            }

            // --- Include edges ---
            foreach (var inc in result.Includes)
            {
                // Coba resolve include path ke file yang ada di snapshot
                var targetFile = ResolveIncludePath(inc.Path, relativePath, snapshot);
                if (targetFile is not null)
                {
                    graph.SourceRelEdges.Add(new GraphEdge
                    {
                        From = filePathId,
                        To = PathId.ForFile(PathId.Normalize(targetFile)),
                        Type = EdgeType.Include
                    });
                }
            }

            // Progress Pass 1: 0-60%
            var pass1Progress = (int)((i + 1.0) / totalFiles * 60);
            yield return new TreeSitterProgress<CodeGraph>
            {
                Percentage = pass1Progress,
                Message = $"Pass 1: {i + 1}/{totalFiles} file diproses — {Path.GetFileName(relativePath)}"
            };

            // Yield control agar tidak memblokir thread caller
            await Task.Yield();
        }

        // ================================================================
        // PASS 2: Usage Scanning (60% - 100%)
        // ================================================================

        yield return new TreeSitterProgress<CodeGraph>
        {
            Percentage = 60,
            Message = "Memulai Pass 2: Usage Scanning..."
        };

        // Index deklarasi by name untuk lookup cepat
        var funcLookup = declaredFunctions
            .GroupBy(d => d.Name)
            .ToDictionary(g => g.Key, g => g.ToList());

        var classLookup = declaredClasses
            .GroupBy(d => d.Name)
            .ToDictionary(g => g.Key, g => g.ToList());

        int fileIdx = 0;
        foreach (var (relativePath, result) in fileResults)
        {
            cancelToken.ThrowIfCancellationRequested();

            // --- Function call edges ---
            foreach (var call in result.Calls)
            {
                var resolved = ResolveSymbol(call.Name, relativePath, result, funcLookup);
                if (resolved is null) continue;

                // Cari caller: function/class di file ini yang mengandung baris call
                var callerPathId = FindCallerPathId(call.Line, relativePath, result);

                graph.UseRelEdges.Add(new GraphEdge
                {
                    From = callerPathId,
                    To = resolved.PathId,
                    Type = EdgeType.Call
                });
            }

            // --- Type reference edges ---
            foreach (var typeRef in result.TypeRefs)
            {
                var resolved = ResolveSymbol(typeRef.Name, relativePath, result, classLookup);
                if (resolved is null) continue;

                var callerPathId = FindCallerPathId(typeRef.Line, relativePath, result);

                graph.UseRelEdges.Add(new GraphEdge
                {
                    From = callerPathId,
                    To = resolved.PathId,
                    Type = EdgeType.Call
                });
            }

            // Progress Pass 2: 60-100%
            fileIdx++;
            var pass2Progress = 60 + (int)((double)fileIdx / fileResults.Count * 40);
            yield return new TreeSitterProgress<CodeGraph>
            {
                Percentage = pass2Progress,
                Message = $"Pass 2: {fileIdx}/{fileResults.Count} file di-scan — {Path.GetFileName(relativePath)}"
            };

            await Task.Yield();
        }

        // === Final yield ===
        yield return new TreeSitterProgress<CodeGraph>
        {
            Percentage = 100,
            Message = $"Analisis selesai. {graph.Nodes.Count} nodes, {graph.SourceRelEdges.Count + graph.UseRelEdges.Count} edges.",
            IsCompleted = true,
            Result = graph
        };
    }

    // ================================================================
    // Private helper methods
    // ================================================================

    /// <summary>
    /// Instantiate lang query provider sesuai bahasa (tanpa factory pattern).
    /// </summary>
    private static BaseLangQuery CreateLangQuery(AnalysisLanguage language) 
        => language switch
        {
            AnalysisLanguage.CSharp => new CSharpLangQuery(),
            AnalysisLanguage.JavaScript => new JavaScriptLangQuery(),
            AnalysisLanguage.Php => new PhpLangQuery(),
            AnalysisLanguage.Cpp => new CppLangQuery(),
            _ => throw new ArgumentOutOfRangeException(nameof(language))
        };

    /// <summary>
    /// Bangun folder/namespace hierarchy nodes dan edges.
    /// </summary>
    private static void BuildFolderHierarchy(
        string relativePath,
        bool usesNamespace,
        LangQueryResult result,
        CodeGraph graph,
        HashSet<string> createdFolderNodes,
        HashSet<string> createdNamespaceNodes)
    {
        // Selalu bangun folder hierarchy dari path segments
        var segments = Path.GetDirectoryName(relativePath)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(segments))
        {
            var parts = segments.Split('/');
            var accumulated = "";

            for (int s = 0; s < parts.Length; s++)
            {
                accumulated = s == 0 ? parts[s] : $"{accumulated}/{parts[s]}";
                var folderId = PathId.ForFolder(accumulated);

                if (createdFolderNodes.Add(folderId))
                {
                    graph.Nodes.Add(new GraphNode
                    {
                        PathId = folderId,
                        Label = parts[s],
                        Type = NodeType.FolderOrNamespace
                    });

                    // Edge ke parent folder
                    if (s > 0)
                    {
                        var parentAccumulated = string.Join('/', parts.Take(s));
                        graph.SourceRelEdges.Add(new GraphEdge
                        {
                            From = PathId.ForFolder(parentAccumulated),
                            To = folderId,
                            Type = EdgeType.BelongsTo
                        });
                    }
                }
            }
        }

        // Jika bahasa pakai namespace, bangun juga namespace nodes
        if (usesNamespace)
        {
            foreach (var ns in result.Namespaces)
            {
                var nsId = PathId.ForNamespace(ns.Name);
                if (createdNamespaceNodes.Add(nsId))
                {
                    graph.Nodes.Add(new GraphNode
                    {
                        PathId = nsId,
                        Label = ns.Name,
                        Type = NodeType.FolderOrNamespace
                    });

                    // Buat edge dari parent namespace segment jika nested
                    var nsParts = ns.Name.Split('.');
                    if (nsParts.Length > 1)
                    {
                        var parentNs = string.Join('.', nsParts.Take(nsParts.Length - 1));
                        var parentNsId = PathId.ForNamespace(parentNs);

                        // Buat parent namespace juga jika belum ada
                        if (createdNamespaceNodes.Add(parentNsId))
                        {
                            graph.Nodes.Add(new GraphNode
                            {
                                PathId = parentNsId,
                                Label = parentNs,
                                Type = NodeType.FolderOrNamespace
                            });
                        }

                        graph.SourceRelEdges.Add(new GraphEdge
                        {
                            From = parentNsId,
                            To = nsId,
                            Type = EdgeType.BelongsTo
                        });
                    }
                }
            }
        }
    }

    /// <summary>
    /// Ambil parent folder path dari relative path file.
    /// </summary>
    private static string? GetParentFolder(string relativePath)
    {
        var dir = Path.GetDirectoryName(relativePath)?.Replace('\\', '/');
        return string.IsNullOrEmpty(dir) ? null : dir;
    }

    /// <summary>
    /// Gabungkan namespace dan class menjadi symbol path.
    /// Contoh: "App.Services" + "UserService" → "App.Services.UserService"
    /// </summary>
    private static string? BuildSymbolPath(string? ns, string? cls)
    {
        if (string.IsNullOrEmpty(ns) && string.IsNullOrEmpty(cls))
            return null;
        if (string.IsNullOrEmpty(ns))
            return cls;
        if (string.IsNullOrEmpty(cls))
            return ns;
        return $"{ns}.{cls}";
    }

    /// <summary>
    /// Resolve scope: cari deklarasi yang cocok dengan nama symbol.
    /// Strategi konservatif:
    ///   1. Same file → langsung match
    ///   2. Same namespace → match jika unik
    ///   3. Global → match jika unik (hanya 1 deklarasi dengan nama itu)
    ///   4. Ambiguous → skip (return null)
    /// </summary>
    private static DeclaredSymbol? ResolveSymbol(
        string name,
        string callerFilePath,
        LangQueryResult callerResult,
        Dictionary<string, List<DeclaredSymbol>> lookup)
    {
        if (!lookup.TryGetValue(name, out var candidates) || candidates.Count == 0)
            return null;

        // 1. Same file — cocok langsung
        var sameFile = candidates.Where(c => c.FilePath == callerFilePath).ToList();
        if (sameFile.Count == 1)
            return sameFile[0];

        // 2. Same namespace
        if (callerResult.Namespaces.Count > 0)
        {
            var callerNs = callerResult.Namespaces[0].Name;
            var sameNs = candidates.Where(c => c.Namespace == callerNs).ToList();
            if (sameNs.Count == 1)
                return sameNs[0];
        }

        // 3. Global — hanya jika unik
        if (candidates.Count == 1)
            return candidates[0];

        // 4. Ambiguous — skip secara konservatif
        return null;
    }

    /// <summary>
    /// Cari PathId caller berdasarkan baris pemanggilan.
    /// Prioritas: function yang mengandung baris > class > file.
    /// </summary>
    private static string FindCallerPathId(
        int callLine,
        string relativePath,
        LangQueryResult result)
    {
        // Cari function terdalam yang mengandung baris ini
        var func = result.Functions
            .Where(f => callLine >= f.StartLine && callLine <= f.EndLine)
            .OrderByDescending(f => f.StartLine)
            .FirstOrDefault();

        if (func is not null)
        {
            var funcLabel = PathId.FormatFunction(func.Name, func.Params);
            var symbolPath = BuildSymbolPath(func.ParentNamespace, func.ParentClass);
            return PathId.Build(relativePath, symbolPath, funcLabel);
        }

        // Cari class terdalam
        var cls = result.Classes
            .Where(c => callLine >= c.StartLine && callLine <= c.EndLine)
            .OrderByDescending(c => c.StartLine)
            .FirstOrDefault();

        if (cls is not null)
            return PathId.Build(relativePath, cls.ParentNamespace, cls.Name);

        // Fallback ke file
        return PathId.ForFile(relativePath);
    }

    /// <summary>
    /// Coba resolve include/import path ke file dalam snapshot.
    /// </summary>
    private static string? ResolveIncludePath(
        string includePath,
        string currentFilePath,
        CodebaseSnapshot snapshot)
    {
        var normalized = PathId.Normalize(includePath);

        // Coba match langsung
        var match = snapshot.Files.FirstOrDefault(f =>
            PathId.Normalize(f.RelativePath).EndsWith(normalized, StringComparison.OrdinalIgnoreCase));

        if (match is not null)
            return match.RelativePath;

        // Coba relative terhadap current file
        var currentDir = Path.GetDirectoryName(currentFilePath)?.Replace('\\', '/') ?? "";
        var resolvedPath = Path.Combine(currentDir, normalized).Replace('\\', '/');

        return snapshot.Files
            .FirstOrDefault(f => PathId.Normalize(f.RelativePath)
                .Equals(resolvedPath, StringComparison.OrdinalIgnoreCase))
            ?.RelativePath;
    }

    // === Internal record untuk tracking deklarasi ===

    private sealed record DeclaredSymbol(
        string Name,
        string PathId,
        string FilePath,
        string? Namespace,
        string? ParentClass = null);

    public void Dispose()
    {
        // Resources (BaseLangQuery) dibuang via using di AnalyzeAsync
    }
}
