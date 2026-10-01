using TreeSitter.CodeGraph.Domain.TreeSitter;

namespace TreeSitter.CodeGraph.Domain.Languages;

/// <summary>
/// Definisi metadata satu bahasa pemrograman yang didukung untuk analisis.
/// </summary>
public sealed record LanguageDefinition(
    string DisplayName,
    AnalysisLanguage Language,
    IReadOnlyList<string> FileExtensions,
    string TreeSitterLanguageId);

