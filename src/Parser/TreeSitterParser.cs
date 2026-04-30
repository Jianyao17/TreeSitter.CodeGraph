using TreeSitter;

namespace GithubAnalyzer.Analysis.Parsers;

public class TreeSitterParser : ICodeParser, IDisposable
{
    private readonly TreeSitter.Parser _parser;
    private readonly Language _language;

    public TreeSitterParser()
    {
        // The loader adds 'tree-sitter-' prefix.
        // File on disk: tree-sitter-c-sharp.dll
        // Providing 'c-sharp' results in 'tree-sitter-c-sharp'
        _language = new Language("c-sharp");
        _parser = new TreeSitter.Parser(_language);
    }

    public object Parse(string code)
    {
        // Parse the code and return the tree. 
        // Note: The Tree object must be kept alive while its Nodes are being accessed.
        var tree = _parser.Parse(code);
        return tree ?? throw new Exception("Failed to parse code.");
    }

    public void Dispose()
    {
        _parser.Dispose();
        _language.Dispose();
    }
}
