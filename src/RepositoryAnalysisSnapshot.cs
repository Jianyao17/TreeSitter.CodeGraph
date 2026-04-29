namespace GithubAnalyzer.Analysis;

public sealed record RepositoryAnalysisSnapshot(
    string Repository,
    int FileCount,
    int NodeCount,
    int EdgeCount,
    IReadOnlyList<CodeGraphNode> Nodes,
    IReadOnlyList<CodeGraphEdge> Edges);

public sealed record CodeGraphNode(
    string Id,
    string Label,
    string Kind);

public sealed record CodeGraphEdge(
    string Source,
    string Target,
    string Relationship);
