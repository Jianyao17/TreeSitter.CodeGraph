namespace GithubAnalyzer.Analysis;

public static class RepositoryAnalysisFactory
{
    public static RepositoryAnalysisSnapshot CreateSample()
    {
        var nodes = new[]
        {
            new CodeGraphNode("repo", "Github-Analyzer", "repository"),
            new CodeGraphNode("api", "Github-Analyzer.WebApi", "service"),
            new CodeGraphNode("frontend", "Github-Analyzer.WebApp", "frontend"),
            new CodeGraphNode("analysis", "Github-Analyzer.Analysis", "module")
        };

        var edges = new[]
        {
            new CodeGraphEdge("frontend", "api", "calls"),
            new CodeGraphEdge("api", "analysis", "uses"),
            new CodeGraphEdge("repo", "api", "contains"),
            new CodeGraphEdge("repo", "frontend", "contains")
        };

        return new RepositoryAnalysisSnapshot(
            Repository: "octocat/Hello-World",
            FileCount: 12,
            NodeCount: nodes.Length,
            EdgeCount: edges.Length,
            Nodes: nodes,
            Edges: edges);
    }
}
