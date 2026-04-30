using GithubAnalyzer.Analysis.Domain;

namespace GithubAnalyzer.Analysis.Analyzer;

public class SimpleAnalyzer : ICodeAnalyzer
{
    public CodeGraph Analyze(object parsedCode, string filePath)
    {
        var graph = new CodeGraph();

        // Add file node
        var fileId = Guid.NewGuid().ToString();
        graph.Nodes.Add(new GraphNode(fileId, filePath, "File"));

        // Add dummy function node
        var funcId = Guid.NewGuid().ToString();
        graph.Nodes.Add(new GraphNode(funcId, "ProcessData", "Function"));

        // Add edge DEFINE from file to function
        graph.Edges.Add(new GraphEdge(fileId, funcId, "DEFINE"));

        return graph;
    }
}
