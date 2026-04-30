using TreeSitter;
using GithubAnalyzer.Analysis.Domain;

namespace GithubAnalyzer.Analysis.Analyzer;

public class TreeSitterAnalyzer : ICodeAnalyzer
{
    public CodeGraph Analyze(object parsedCode, string filePath)
    {
        var graph = new CodeGraph();
        var fileId = Guid.NewGuid().ToString();
        graph.Nodes.Add(new GraphNode(fileId, filePath, "File"));

        if (parsedCode is Tree tree)
        {
            Traverse(tree.RootNode, fileId, graph);
        }
        else if (parsedCode is Node rootNode)
        {
            Traverse(rootNode, fileId, graph);
        }

        return graph;
    }

    private void Traverse(Node node, string parentId, CodeGraph graph)
    {
        // Simple heuristic: Look for method declarations
        // In Tree-sitter C# grammar, these are often "method_declaration"
        if (node.Type == "method_declaration")
        {
            var methodName = FindChildByType(node, "identifier")?.Text ?? "UnknownMethod";
            var methodId = Guid.NewGuid().ToString();
            
            graph.Nodes.Add(new GraphNode(methodId, methodName, "Method"));
            graph.Edges.Add(new GraphEdge(parentId, methodId, "CONTAINS"));
            
            // We could go deeper, but let's keep it simple
            return; 
        }

        // Continue traversal using Children enumerable
        foreach (var child in node.Children)
        {
            Traverse(child, parentId, graph);
        }
    }

    private Node? FindChildByType(Node node, string type)
    {
        foreach (var child in node.Children)
        {
            if (child.Type == type) return child;
        }
        return null;
    }
}
