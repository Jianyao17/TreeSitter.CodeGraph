using GithubAnalyzer.Analysis.Domain.TreeSitter;

namespace GithubAnalyzer.Analysis.Domain.Languages;

/// <summary>
/// Definisi metadata satu bahasa pemrograman yang didukung untuk analisis.
/// </summary>
public sealed record LanguageDefinition(
    AnalysisLanguage Language,
    string DisplayName,
    IReadOnlyList<string> FileExtensions,
    string TreeSitterLanguageId);
