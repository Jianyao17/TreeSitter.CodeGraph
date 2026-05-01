namespace GithubAnalyzer.Analysis.Graph;

public sealed class CodeGraph
{
    public List<GraphNode> Nodes { get; init; } = new();
    public List<GraphEdge> Edges { get; init; } = new();
}
