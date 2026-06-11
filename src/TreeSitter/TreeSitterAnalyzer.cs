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
    // === Class-level analysis state ===
    private CodeGraph _graph = new();                                            // Output graf akhir
    private readonly List<SymbolDeclaration> _declaredFunctions = [];            // Deklarasi fungsi dari Pass 1, untuk lookup di Pass 2
    private readonly List<SymbolDeclaration> _declaredClasses = [];              // Deklarasi class dari Pass 1, untuk lookup di Pass 2
    private readonly Dictionary<string, LangQueryResult> _fileResults = new();   // Cache hasil query per file, reuse di Pass 2
    private readonly HashSet<string> _createdDirNodes = new();                   // Tracking directory nodes yang sudah dibuat (anti-duplikat)
    private readonly HashSet<string> _createdNsNodes = new();                    // Tracking namespace nodes yang sudah dibuat (anti-duplikat)

    public async IAsyncEnumerable<TreeSitterProgress<CodeGraph>> AnalyzeAsync(
        CodebaseSnapshot snapshot,
        AnalysisLanguage language,
        [EnumeratorCancellation]
        CancellationToken cancelToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        // Reset state untuk analisis baru
        ResetState();

        using var langQuery = CreateLangQuery(language);
        var totalFiles = snapshot.Files.Count;

        if (totalFiles == 0)
        {
            yield return new TreeSitterProgress<CodeGraph>
            {
                Percentage = 100,
                Message = "Tidak ada file untuk dianalisis.",
                IsCompleted = true,
                Result = _graph
            };
            yield break;
        }

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

            LangQueryResult result;
            try
            {
                result = langQuery.ExtractAll(file.Content);
            }
            catch (Exception)
            {
                // Skip file yang gagal di-parse (binary, corrupt, dll.)
                continue;
            }

            // Cache hasil query per file untuk Pass 2 (usage scanning)
            _fileResults[relativePath] = result;

            // Proses deklarasi: bangun hierarchy nodes, file node, dan SourceRelEdges
            ProcessDeclarations(relativePath, result, langQuery.UsesNamespace, snapshot);

            // Progress Pass 1: 0-60%
            var pass1Progress = (int)((i + 1.0) / totalFiles * 60);
            yield return new TreeSitterProgress<CodeGraph>
            {
                Percentage = pass1Progress,
                Message = $"Pass 1: {i + 1}/{totalFiles} file diproses — {Path.GetFileName(relativePath)}"
            };

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

        // Index deklarasi berdasarkan nama untuk lookup cepat
        var funcLookup = _declaredFunctions
            .GroupBy(d => d.Name)
            .ToDictionary(g => g.Key, g => g.ToList());

        var classLookup = _declaredClasses
            .GroupBy(d => d.Name)
            .ToDictionary(g => g.Key, g => g.ToList());

        int fileIdx = 0;
        foreach (var (relativePath, result) in _fileResults)
        {
            cancelToken.ThrowIfCancellationRequested();

            // Proses usage: bangun UseRelEdges berdasarkan calls dan typeRefs
            ProcessUsages(relativePath, result, funcLookup, classLookup);

            fileIdx++;
            var pass2Progress = 60 + (int)((double)fileIdx / _fileResults.Count * 40);
            yield return new TreeSitterProgress<CodeGraph>
            {
                Percentage = pass2Progress,
                Message = $"Pass 2: {fileIdx}/{_fileResults.Count} file di-scan — {Path.GetFileName(relativePath)}"
            };

            await Task.Yield();
        }

        // === Final yield ===
        yield return new TreeSitterProgress<CodeGraph>
        {
            Percentage = 100,
            Message = $"Analisis selesai. {_graph.Nodes.Count} nodes, "
                    + $"{_graph.SourceRelEdges.Count + _graph.UseRelEdges.Count} edges.",
            IsCompleted = true,
            Result = _graph
        };
    }

    // ================================================================
    // Pass 1: Declaration Mapping
    // ================================================================

    /// <summary>
    /// Proses satu file: bangun hierarchy, file/class/function nodes, dan SourceRelEdges.
    /// </summary>
    private void ProcessDeclarations(
        string relativePath, LangQueryResult result,
        bool usesNamespace, CodebaseSnapshot snapshot)
    {
        // --- Directory hierarchy ---
        BuildDirectoryHierarchy(relativePath);

        // --- Namespace hierarchy (jika bahasa mendukung) ---
        if (usesNamespace)
            BuildNamespaceHierarchy(result);

        // --- File node ---
        var filePathId = PathId.ForFile(relativePath);
        _graph.Nodes.Add(new GraphNode
        {
            PathId = filePathId,
            Label = Path.GetFileName(relativePath),
            Type = NodeType.File
        });

        // Edge: parent directory → file (BelongsTo)
        var parentDir = GetParentDirectory(relativePath);
        if (!string.IsNullOrEmpty(parentDir))
        {
            _graph.SourceRelEdges.Add(new GraphEdge
            {
                From = PathId.ForDirectory(parentDir),
                To = filePathId,
                Type = EdgeType.BelongsTo
            });
        }

        // --- Class nodes ---
        foreach (var cls in result.Classes)
        {
            var symbolPath = BuildSymbolPath(cls.ParentNamespace, cls.ParentChain);
            var classPathId = PathId.Build(relativePath, symbolPath, cls.Name);

            _graph.Nodes.Add(new GraphNode
            {
                PathId = classPathId,
                Label = cls.Name,
                Type = NodeType.Class,
                StartLine = cls.StartLine + 1,
                EndLine = cls.EndLine + 1
            });

            // Edge: parent → class (Define)
            if (!string.IsNullOrEmpty(cls.ParentChain))
            {
                // Nested class: parent = immediate parent container
                var parentOfChain = GetParentOfChain(cls.ParentChain);
                var immediateParent = GetLastSegment(cls.ParentChain);
                var parentSymbolPath = BuildSymbolPath(cls.ParentNamespace, parentOfChain);
                var classParentId = PathId.Build(relativePath, parentSymbolPath, immediateParent);
                
                _graph.SourceRelEdges.Add(new GraphEdge
                {
                    From = classParentId,
                    To = classPathId,
                    Type = EdgeType.Define
                });
            }
            else
            {
                // Top-level class
                // 1. Edge dari Namespace (jika bahasa menggunakan namespace)
                if (usesNamespace && !string.IsNullOrEmpty(cls.ParentNamespace))
                {
                    _graph.SourceRelEdges.Add(new GraphEdge
                    {
                        From = PathId.ForNamespace(cls.ParentNamespace),
                        To = classPathId,
                        Type = EdgeType.Define
                    });
                }
                
                // 2. Edge dari File (selalu dibuat agar directory-based view bekerja)
                _graph.SourceRelEdges.Add(new GraphEdge
                {
                    From = filePathId,
                    To = classPathId,
                    Type = EdgeType.Define
                });
            }

            _declaredClasses.Add(new SymbolDeclaration(
                cls.Name, classPathId, relativePath, cls.ParentNamespace));
        }

        // --- Function nodes ---
        foreach (var func in result.Functions)
        {
            var funcLabel = PathId.FormatFunction(func.Name, func.Params);
            var symbolPath = BuildSymbolPath(func.ParentNamespace, func.ParentChain);
            var funcPathId = PathId.Build(relativePath, symbolPath, funcLabel);

            _graph.Nodes.Add(new GraphNode
            {
                PathId = funcPathId,
                Label = funcLabel,
                Type = NodeType.Function,
                StartLine = func.StartLine + 1,
                EndLine = func.EndLine + 1
            });

            // Edge: parent → function (Define)
            if (!string.IsNullOrEmpty(func.ParentChain))
            {
                // Function inside class/function: parent = immediate parent container
                var parentOfChain = GetParentOfChain(func.ParentChain);
                var immediateParent = GetLastSegment(func.ParentChain);
                var parentSymbolPath = BuildSymbolPath(func.ParentNamespace, parentOfChain);
                var parentId = PathId.Build(relativePath, parentSymbolPath, immediateParent);
                
                _graph.SourceRelEdges.Add(new GraphEdge
                {
                    From = parentId,
                    To = funcPathId,
                    Type = EdgeType.Define
                });
            }
            else
            {
                // Top-level function
                // 1. Edge dari Namespace (jika bahasa menggunakan namespace)
                if (usesNamespace && !string.IsNullOrEmpty(func.ParentNamespace))
                {
                    _graph.SourceRelEdges.Add(new GraphEdge
                    {
                        From = PathId.ForNamespace(func.ParentNamespace),
                        To = funcPathId,
                        Type = EdgeType.Define
                    });
                }
                
                // 2. Edge dari File (selalu dibuat agar directory-based view bekerja)
                _graph.SourceRelEdges.Add(new GraphEdge
                {
                    From = filePathId,
                    To = funcPathId,
                    Type = EdgeType.Define
                });
            }

            _declaredFunctions.Add(new SymbolDeclaration(
                func.Name, funcPathId, relativePath, func.ParentNamespace, func.ParentChain));
        }

        // --- Include edges ---
        foreach (var inc in result.Includes)
        {
            var targetFile = ResolveIncludePath(inc.Path, relativePath, snapshot);
            if (targetFile is not null)
            {
                _graph.SourceRelEdges.Add(new GraphEdge
                {
                    From = filePathId,
                    To = PathId.ForFile(PathId.Normalize(targetFile)),
                    Type = EdgeType.Include
                });
            }
        }
    }

    // ================================================================
    // Pass 2: Usage Scanning
    // ================================================================

    /// <summary>
    /// Proses satu file: resolve calls/refs dan build UseRelEdges.
    /// Arah: From = sumber definisi → To = tempat dipanggil.
    /// </summary>
    private void ProcessUsages(
        string relativePath,
        LangQueryResult result,
        Dictionary<string, List<SymbolDeclaration>> funcLookup,
        Dictionary<string, List<SymbolDeclaration>> classLookup)
    {
        // --- Function call edges ---
        foreach (var call in result.Calls)
        {
            var resolved = ResolveSymbol(call.Name, relativePath, result, funcLookup);
            if (resolved is null) continue;

            var callerPathId = FindCallerPathId(call.Line, relativePath, result);

            _graph.UseRelEdges.Add(new GraphEdge
            {
                From = resolved.PathId,    // sumber definisi
                To = callerPathId,         // tempat dipanggil
                Type = EdgeType.Call
            });
        }

        // --- Type reference edges ---
        foreach (var typeRef in result.TypeRefs)
        {
            var resolved = ResolveSymbol(typeRef.Name, relativePath, result, classLookup);
            if (resolved is null) continue;

            var callerPathId = FindCallerPathId(typeRef.Line, relativePath, result);

            _graph.UseRelEdges.Add(new GraphEdge
            {
                From = resolved.PathId,    // sumber definisi
                To = callerPathId,         // tempat dipanggil
                Type = EdgeType.Call
            });
        }
    }

    // ================================================================
    // Hierarchy builders
    // ================================================================

    /// <summary>
    /// Bangun directory hierarchy nodes dan edges dari path segments.
    /// </summary>
    private void BuildDirectoryHierarchy(string relativePath)
    {
        var dirPath = Path.GetDirectoryName(relativePath)?.Replace('\\', '/');
        if (string.IsNullOrEmpty(dirPath)) return;

        var parts = dirPath.Split('/');
        var accumulated = "";

        for (int s = 0; s < parts.Length; s++)
        {
            accumulated = s == 0 ? parts[s] : $"{accumulated}/{parts[s]}";
            var dirId = PathId.ForDirectory(accumulated);

            if (_createdDirNodes.Add(dirId))
            {
                _graph.Nodes.Add(new GraphNode
                {
                    PathId = dirId,
                    Label = parts[s],
                    Type = NodeType.Directory
                });

                // Edge: parent directory → child directory (BelongsTo)
                if (s > 0)
                {
                    var parentAccumulated = string.Join('/', parts.Take(s));
                    _graph.SourceRelEdges.Add(new GraphEdge
                    {
                        Type = EdgeType.BelongsTo,
                        From = PathId.ForDirectory(parentAccumulated),
                        To = dirId
                    });
                }
            }
        }
    }

    /// <summary>
    /// Bangun namespace hierarchy nodes dan edges.
    /// Membuat semua intermediate namespace nodes (mirip BuildDirectoryHierarchy).
    /// Contoh: "A.B.C" → nodes: ::A, ::A.B, ::A.B.C dengan edges BelongsTo antar level.
    /// </summary>
    private void BuildNamespaceHierarchy(LangQueryResult result)
    {
        foreach (var ns in result.Namespaces)
        {
            var nsParts = ns.Name.Split('.');
            var accumulated = "";

            for (int i = 0; i < nsParts.Length; i++)
            {
                accumulated = i == 0 ? nsParts[i] : $"{accumulated}.{nsParts[i]}";
                var currentNsId = PathId.ForNamespace(accumulated);

                if (_createdNsNodes.Add(currentNsId))
                {
                    _graph.Nodes.Add(new GraphNode
                    {
                        PathId = currentNsId,
                        Label = nsParts[i],
                        Type = NodeType.Namespace
                    });

                    // Edge: parent namespace → child namespace (BelongsTo)
                    if (i > 0)
                    {
                        var parentAccumulated = string.Join('.', nsParts.Take(i));
                        _graph.SourceRelEdges.Add(new GraphEdge
                        {
                            From = PathId.ForNamespace(parentAccumulated),
                            To = currentNsId,
                            Type = EdgeType.BelongsTo
                        });
                    }
                }
            }
        }
    }

    // ================================================================
    // Private helper methods
    // ================================================================

    /// <summary>
    /// Reset semua state untuk analisis baru.
    /// </summary>
    private void ResetState()
    {
        _graph = new CodeGraph();
        _declaredFunctions.Clear();
        _declaredClasses.Clear();
        _fileResults.Clear();
        _createdDirNodes.Clear();
        _createdNsNodes.Clear();
    }

    /// <summary>
    /// Instantiate lang query provider sesuai bahasa (tanpa factory pattern).
    /// </summary>
    private static BaseLangQuery CreateLangQuery(AnalysisLanguage language)
        => language switch
        {
            AnalysisLanguage.CSharp     => new CSharpLangQuery(),
            AnalysisLanguage.JavaScript => new JavaScriptLangQuery(),
            AnalysisLanguage.Php        => new PhpLangQuery(),
            AnalysisLanguage.Cpp        => new CppLangQuery(),

            _ => throw new ArgumentOutOfRangeException(nameof(language))
        };

    /// <summary>
    /// Ambil parent directory path dari relative path file.
    /// </summary>
    private static string? GetParentDirectory(string relativePath)
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
    private static SymbolDeclaration? ResolveSymbol(
        string name,
        string callerFilePath,
        LangQueryResult callerResult,
        Dictionary<string, List<SymbolDeclaration>> lookup)
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
            var symbolPath = BuildSymbolPath(func.ParentNamespace, func.ParentChain);
            return PathId.Build(relativePath, symbolPath, funcLabel);
        }

        // Cari class terdalam
        var cls = result.Classes
            .Where(c => callLine >= c.StartLine && callLine <= c.EndLine)
            .OrderByDescending(c => c.StartLine)
            .FirstOrDefault();

        if (cls is not null)
        {
            var symbolPath = BuildSymbolPath(cls.ParentNamespace, cls.ParentChain);
            return PathId.Build(relativePath, symbolPath, cls.Name);
        }

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

    // === Internal record ===

    private sealed record SymbolDeclaration(
        string Name,
        string PathId,
        string FilePath,
        string? Namespace,
        string? ParentChain = null);

    // === Chain helpers ===

    /// <summary>
    /// "UserService.User" → "UserService", "UserService" → null
    /// </summary>
    private static string? GetParentOfChain(string chain)
    {
        var idx = chain.LastIndexOf('.');
        return idx < 0 ? null : chain[..idx];
    }

    /// <summary>
    /// "UserService.User" → "User", "UserService" → "UserService"
    /// </summary>
    private static string GetLastSegment(string chain)
    {
        var idx = chain.LastIndexOf('.');
        return idx < 0 ? chain : chain[(idx + 1)..];
    }

    public void Dispose()
    {
        // Resources (BaseLangQuery) dibuang via using di AnalyzeAsync
    }
}
