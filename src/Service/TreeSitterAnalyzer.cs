using TreeSitter;
using GithubAnalyzer.Analysis.Domain.Graph;
using GithubAnalyzer.Analysis.Interface;

namespace GithubAnalyzer.Analysis.Service;

public class TreeSitterAnalyzer : ICodeAnalyzer
{
    public CodeGraph Analyze(object parsedCode, string filePath)
    {
        var graph = new CodeGraph();
        var fileId = Guid.NewGuid().ToString();
        graph.Nodes.Add(new GraphNode
        {
            PathId = fileId,
            Label = Path.GetFileName(filePath),
            Type = NodeType.File
        });

        if (parsedCode is Tree tree)
        {
            Traverse(tree.RootNode, fileId, filePath, graph);
        }
        else if (parsedCode is Node rootNode)
        {
            Traverse(rootNode, fileId, filePath, graph);
        }

        return graph;
    }

    private void Traverse(Node node, string parentId, string filePath, CodeGraph graph)
    {
        // Simple heuristic: Look for method declarations
        // In Tree-sitter C# grammar, these are often "method_declaration"
        if (node.Type == "method_declaration")
        {
            var methodName = FindChildByType(node, "identifier")?.Text ?? "UnknownMethod";
            var methodId = Guid.NewGuid().ToString();

            graph.Nodes.Add(new GraphNode
            {
                PathId = methodId,
                Label = methodName,
                Type = NodeType.Function
            });
            graph.SourceRelEdges.Add(new GraphEdge
            {
                From = parentId,
                To = methodId,
                Type = EdgeType.Define
            });

            // We could go deeper, but let's keep it simple
            return;
        }

        // Continue traversal using Children enumerable
        foreach (var child in node.Children)
        {
            Traverse(child, parentId, filePath, graph);
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