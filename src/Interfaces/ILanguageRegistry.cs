using GithubAnalyzer.Analysis.Domain.Languages;
using GithubAnalyzer.Analysis.Domain.TreeSitter;

namespace GithubAnalyzer.Analysis.Interfaces;

/// <summary>
/// Kontrak untuk katalog bahasa pemrograman yang didukung oleh analyzer.
/// </summary>
public interface ILanguageRegistry
{
    /// <summary>
    /// Mendapatkan daftar semua definisi bahasa yang didukung.
    /// </summary>
    IReadOnlyCollection<LanguageDefinition> GetAllLanguages();

    /// <summary>
    /// Mendapatkan definisi metadata untuk bahasa tertentu.
    /// </summary>
    LanguageDefinition GetDefinition(AnalysisLanguage language);

    /// <summary>
    /// Mencari definisi bahasa berdasarkan ekstensi file (contoh: ".cs", "ts", ".php").
    /// </summary>
    LanguageDefinition? FindByExtension(string fileExtension);

    /// <summary>
    /// Mendapatkan daftar ekstensi file yang didukung untuk bahasa tertentu.
    /// </summary>
    IReadOnlyCollection<string> GetExtensions(AnalysisLanguage language);

    /// <summary>
    /// Mendapatkan Tree-Sitter language identifier string untuk bahasa tertentu.
    /// </summary>
    string GetTreeSitterLanguageId(AnalysisLanguage language);
}
