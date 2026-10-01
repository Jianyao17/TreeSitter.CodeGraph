using TreeSitter.CodeGraph.Domain.Languages;
using TreeSitter.CodeGraph.Domain.TreeSitter;
using TreeSitter.CodeGraph.Interfaces;

namespace TreeSitter.CodeGraph.Languages;

/// <summary>
/// Extension methods ergonomis untuk enum AnalysisLanguage.
/// </summary>
public static class AnalysisLanguageExtensions
{
    /// <summary>
    /// Mendapatkan daftar ekstensi file yang didukung untuk bahasa ini.
    /// </summary>
    public static IReadOnlyCollection<string> GetSupportedExtensions(
        this AnalysisLanguage language,
        ILanguageRegistry? registry = null)
    {
        return (registry ?? LanguageRegistry.Default).GetExtensions(language);
    }

    /// <summary>
    /// Mendapatkan Tree-Sitter language identifier string untuk bahasa ini.
    /// </summary>
    public static string GetTreeSitterId(
        this AnalysisLanguage language,
        ILanguageRegistry? registry = null)
    {
        return (registry ?? LanguageRegistry.Default).GetTreeSitterLanguageId(language);
    }

    /// <summary>
    /// Mendapatkan nama tampilan (Display Name) resmi untuk bahasa ini.
    /// </summary>
    public static string GetDisplayName(
        this AnalysisLanguage language,
        ILanguageRegistry? registry = null)
    {
        return (registry ?? LanguageRegistry.Default).GetDefinition(language).DisplayName;
    }

    /// <summary>
    /// Mendapatkan definisi metadata lengkap untuk bahasa ini.
    /// </summary>
    public static LanguageDefinition GetDefinition(
        this AnalysisLanguage language,
        ILanguageRegistry? registry = null)
    {
        return (registry ?? LanguageRegistry.Default).GetDefinition(language);
    }
}

