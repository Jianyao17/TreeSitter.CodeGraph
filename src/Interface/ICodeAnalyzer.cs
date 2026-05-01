using GithubAnalyzer.Analysis.Graph;

namespace GithubAnalyzer.Analysis.Interface;

public interface ICodeAnalyzer
{
    CodeGraph Analyze(object parsedCode, string filePath);
}