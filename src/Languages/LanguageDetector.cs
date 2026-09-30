using GithubAnalyzer.Analysis.Domain.Languages;
using GithubAnalyzer.Analysis.Domain.TreeSitter;
using GithubAnalyzer.Analysis.Interfaces;

namespace GithubAnalyzer.Analysis.Languages;

/// <summary>
/// Engine pendeteksi bahasa pemrograman dominan pada codebase.
/// Mendukung traversal filesystem maupun input kumpulan file in-memory.
/// </summary>
public sealed class LanguageDetector : ILanguageDetector
{
    private readonly ILanguageRegistry _registry;

    public LanguageDetector()
        : this(LanguageRegistry.Default)
    {
    }

    public LanguageDetector(ILanguageRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <inheritdoc />
    public LanguageDetectionResult Detect(string directoryPath, LanguageDetectionOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentException("Directory path must not be null or empty.", nameof(directoryPath));
        }

        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }

        options ??= new LanguageDetectionOptions();
        var excludedSet = new HashSet<string>(options.ExcludedFolders, StringComparer.OrdinalIgnoreCase);

        var counts = InitializeCounts();
        int totalFilesScanned = 0;
        int totalMatched = 0;

        var dirsToProcess = new Stack<string>();
        dirsToProcess.Push(directoryPath);

        while (dirsToProcess.Count > 0)
        {
            var currentDir = dirsToProcess.Pop();

            try
            {
                foreach (var file in Directory.EnumerateFiles(currentDir))
                {
                    totalFilesScanned++;
                    var ext = Path.GetExtension(file);
                    var def = _registry.FindByExtension(ext);
                    if (def != null)
                    {
                        counts[def.Language]++;
                        totalMatched++;
                    }
                }

                foreach (var subDir in Directory.EnumerateDirectories(currentDir))
                {
                    var dirName = Path.GetFileName(subDir);
                    if (!excludedSet.Contains(dirName) && !dirName.StartsWith('.'))
                    {
                        dirsToProcess.Push(subDir);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Lewati folder yang tidak memiliki izin akses
            }
        }

        return BuildResult(counts, totalMatched, totalFilesScanned, options.DefaultFallback);
    }

    /// <inheritdoc />
    public Task<LanguageDetectionResult> DetectAsync(
        string directoryPath,
        LanguageDetectionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Detect(directoryPath, options);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public LanguageDetectionResult DetectFromFiles(
        IEnumerable<string> filePaths,
        LanguageDetectionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(filePaths);

        options ??= new LanguageDetectionOptions();
        var counts = InitializeCounts();
        int totalFilesScanned = 0;
        int totalMatched = 0;

        foreach (var file in filePaths)
        {
            if (string.IsNullOrWhiteSpace(file)) continue;

            totalFilesScanned++;
            var ext = Path.GetExtension(file);
            var def = _registry.FindByExtension(ext);
            if (def != null)
            {
                counts[def.Language]++;
                totalMatched++;
            }
        }

        return BuildResult(counts, totalMatched, totalFilesScanned, options.DefaultFallback);
    }

    private Dictionary<AnalysisLanguage, int> InitializeCounts()
    {
        var counts = new Dictionary<AnalysisLanguage, int>();
        foreach (var lang in _registry.GetAllLanguages())
        {
            counts[lang.Language] = 0;
        }
        return counts;
    }

    private static LanguageDetectionResult BuildResult(
        Dictionary<AnalysisLanguage, int> counts,
        int totalMatched,
        int totalFilesScanned,
        AnalysisLanguage? fallback)
    {
        AnalysisLanguage? primary = null;

        if (totalMatched > 0)
        {
            primary = counts
                .OrderByDescending(x => x.Value)
                .First()
                .Key;
        }
        else
        {
            primary = fallback;
        }

        return new LanguageDetectionResult
        {
            PrimaryLanguage = primary,
            TotalMatchedFiles = totalMatched,
            TotalFilesScanned = totalFilesScanned,
            LanguageCounts = counts
        };
    }
}
