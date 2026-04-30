using GithubAnalyzer.Analysis.Domain;

namespace GithubAnalyzer.Analysis.Analyzer;

public interface ICodeAnalyzer
{
    CodeGraph Analyze(object parsedCode, string filePath);
}
