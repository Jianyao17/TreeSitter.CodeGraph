using GithubAnalyzer.Analysis.Domain.Reader;
using GithubAnalyzer.Analysis.Interface;

namespace GithubAnalyzer.Analysis.Reader;

/// <summary>
/// Reader untuk mengambil konten codebase sesuai filter.
/// </summary>
public sealed class CodebaseReader : ICodebaseReader
{
    /// <summary>
    /// Membaca semua file yang lolos filter dan mengembalikan snapshot.
    /// </summary>
    public async Task<CodebaseSnapshot> ReadAsync(
        string rootPath,
        CodebaseReadOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Root path is required.", nameof(rootPath));
        }

        var normalizedExtensions = NormalizeExtensions(options.AllowedExtensions);
        var excludedFolders = new HashSet<string>(options.ExcludedFolders, StringComparer.OrdinalIgnoreCase);

        // Github zip files extract to a single root folder (e.g. Repo-CommitHash).
        // Detect this wrapper and step inside to get the actual repository root.
        var actualRootPath = rootPath;
        if (Directory.Exists(actualRootPath))
        {
            var subDirs = Directory.GetDirectories(actualRootPath);
            var subFiles = Directory.GetFiles(actualRootPath);
            if (subDirs.Length == 1 && subFiles.Length == 0)
            {
                actualRootPath = subDirs[0];
            }
        }

        var snapshot = new CodebaseSnapshot { RootPath = actualRootPath };

        foreach (var filePath in Directory.EnumerateFiles(actualRootPath, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Filter berdasarkan folder yang dikecualikan
            if (IsInExcludedFolder(actualRootPath, filePath, excludedFolders))
                continue;

            // Filter berdasarkan ekstensi file
            var extension = Path.GetExtension(filePath);
            if (normalizedExtensions.Count > 0 && !normalizedExtensions.Contains(extension))
                continue;

            var fileInfo = new FileInfo(filePath);
            if (options.MaxFileSizeBytes.HasValue && fileInfo.Length > options.MaxFileSizeBytes.Value)
                continue; // Filter berdasarkan ukuran file

            var relativePath = Path.GetRelativePath(actualRootPath, filePath);
            var info = new CodebaseFileInfo
            {
                RelativePath = relativePath,
                AbsolutePath = filePath,
                Extension = extension,
                SizeBytes = fileInfo.Length
            };

            if (options.CustomFilter != null && !options.CustomFilter(info))
                continue;

            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            snapshot.Files.Add(new CodebaseFileContent
            {
                RelativePath = relativePath,
                AbsolutePath = filePath,
                Extension = extension,
                SizeBytes = fileInfo.Length,
                Content = content
            });
        }

        return snapshot;
    }

    /// <summary>
    /// Normalisasi ekstensi agar konsisten diawali titik.
    /// </summary>
    private static HashSet<string> NormalizeExtensions(IReadOnlyCollection<string> extensions)
    {
        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var extension in extensions)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                continue;
            }

            normalized.Add(extension.StartsWith('.') ? extension : $".{extension}");
        }

        return normalized;
    }

    /// <summary>
    /// Mengecek apakah file berada di folder yang dikecualikan.
    /// </summary>
    private static bool IsInExcludedFolder(
        string rootPath,
        string filePath,
        HashSet<string> excludedFolders)
    {
        if (excludedFolders.Count == 0)
        {
            return false;
        }

        var relativePath = Path.GetRelativePath(rootPath, filePath);
        var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        foreach (var segment in segments)
        {
            if (excludedFolders.Contains(segment))
            {
                return true;
            }
        }

        return false;
    }
}
