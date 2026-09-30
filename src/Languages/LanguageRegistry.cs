using GithubAnalyzer.Analysis.Domain.Languages;
using GithubAnalyzer.Analysis.Domain.TreeSitter;
using GithubAnalyzer.Analysis.Interfaces;

namespace GithubAnalyzer.Analysis.Languages;

/// <summary>
/// Registry terpusat untuk katalog bahasa pemrograman yang didukung oleh analyzer.
/// Merupakan Single Source of Truth untuk metadata bahasa, ekstensi file, dan Tree-Sitter grammar ID.
/// </summary>
public sealed class LanguageRegistry : ILanguageRegistry
{
    private static readonly Lazy<LanguageRegistry> _default = new(() => new LanguageRegistry());

    /// <summary>
    /// Instance default singleton dari LanguageRegistry.
    /// </summary>
    public static LanguageRegistry Default => _default.Value;

    private readonly Dictionary<AnalysisLanguage, LanguageDefinition> _definitions;
    private readonly Dictionary<string, LanguageDefinition> _extensionLookup;

    /// <summary>
    /// Membuat registry baru dengan bahasa-bahasa bawaan sistem.
    /// </summary>
    public LanguageRegistry()
        : this(GetDefaultDefinitions())
    {
    }

    /// <summary>
    /// Membuat registry kustom dengan koleksi definisi bahasa tertentu.
    /// </summary>
    public LanguageRegistry(IEnumerable<LanguageDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        _definitions = new Dictionary<AnalysisLanguage, LanguageDefinition>();
        _extensionLookup = new Dictionary<string, LanguageDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var def in definitions)
        {
            _definitions[def.Language] = def;
            foreach (var ext in def.FileExtensions)
            {
                var normalizedExt = NormalizeExtension(ext);
                _extensionLookup[normalizedExt] = def;
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyCollection<LanguageDefinition> GetAllLanguages() => _definitions.Values;

    /// <inheritdoc />
    public LanguageDefinition GetDefinition(AnalysisLanguage language)
    {
        if (_definitions.TryGetValue(language, out var def))
        {
            return def;
        }

        throw new ArgumentOutOfRangeException(
            nameof(language),
            language,
            $"Bahasa '{language}' belum didaftarkan di LanguageRegistry.");
    }

    /// <inheritdoc />
    public LanguageDefinition? FindByExtension(string fileExtension)
    {
        if (string.IsNullOrWhiteSpace(fileExtension))
        {
            return null;
        }

        var normalized = NormalizeExtension(fileExtension);
        return _extensionLookup.GetValueOrDefault(normalized);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> GetExtensions(AnalysisLanguage language)
    {
        return GetDefinition(language).FileExtensions;
    }

    /// <inheritdoc />
    public string GetTreeSitterLanguageId(AnalysisLanguage language)
    {
        return GetDefinition(language).TreeSitterLanguageId;
    }

    private static string NormalizeExtension(string extension)
    {
        var trimmed = extension.Trim();
        return trimmed.StartsWith('.') ? trimmed.ToLowerInvariant() : $".{trimmed.ToLowerInvariant()}";
    }

    private static List<LanguageDefinition> GetDefaultDefinitions()
    {
        return
        [
            new LanguageDefinition(
                "C#",
                AnalysisLanguage.CSharp,
                new[] { ".cs" },
                "c-sharp"),

            new LanguageDefinition(
                "JavaScript",
                AnalysisLanguage.JavaScript,
                new[] { ".js", ".ts" },
                "javascript"),

            new LanguageDefinition(
                "PHP",
                AnalysisLanguage.Php,
                new[] { ".php" },
                "php"),

            new LanguageDefinition(
                "C++",
                AnalysisLanguage.Cpp,
                new[] { ".cpp", ".cxx", ".cc", ".h", ".hpp" },
                "cpp")
        ];
    }
}
