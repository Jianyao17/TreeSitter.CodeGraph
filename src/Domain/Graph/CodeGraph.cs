namespace GithubAnalyzer.Analysis.Domain.Graph;

public sealed class CodeGraph
{
    // Daftar node dalam graf kode
    public List<GraphNode> Nodes { get; init; } = new();

    // Daftar edge kategori SourceRelation (struktur / definisi)
    public List<GraphEdge> SourceRelEdges { get; init; } = new();
    
    // Daftar edge kategori UseRelation (penggunaan / dependensi)
    public List<GraphEdge> UseRelEdges { get; init; } = new();
}
