namespace GithubAnalyzer.Analysis.Parsers;

public class DummyParser : ICodeParser
{
    public object Parse(string code)
    {
        // Placeholder implementation
        return new { RawCode = code };
    }
}
