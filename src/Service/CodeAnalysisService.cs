using GithubAnalyzer.Analysis.Interface;

namespace GithubAnalyzer.Analysis.Service;

/// <summary>
/// Service to orchestrate the code analysis process.
/// Example usage:
/// <code>
/// var service = new CodeAnalysisService(
///     new DummyParser(),
///     new SimpleAnalyzer(),
///     new JsonSerializerService()
/// );
/// string jsonResult = service.Analyze("void main() {}", "main.cs");
/// </code>
/// </summary>
public class CodeAnalysisService(
    ICodeParser parser,
    ICodeAnalyzer analyzer,
    JsonSerializerService serializer)
{
    private readonly ICodeParser _parser = parser;
    private readonly ICodeAnalyzer _analyzer = analyzer;
    private readonly JsonSerializerService _serializer = serializer;

    public string Analyze(string code, string filePath)
    {
        // 1. Parsing
        var parsedCode = _parser.Parse(code);

        try
        {
            // 2. Analysis
            var graph = _analyzer.Analyze(parsedCode, filePath);

            // 3. Serialization
            return _serializer.Serialize(graph);
        }
        finally
        {
            if (parsedCode is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}