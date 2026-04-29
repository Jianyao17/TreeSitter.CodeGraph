using GithubAnalyzer.Analysis;

namespace GithubAnalyzer.Analysis.Tests;

public sealed class RepositoryAnalysisFactoryTests
{
    [Fact]
    public void CreateSample_ReturnsConnectedSnapshot()
    {
        var snapshot = RepositoryAnalysisFactory.CreateSample();

        Assert.Equal("octocat/Hello-World", snapshot.Repository);
        Assert.NotEmpty(snapshot.Nodes);
        Assert.NotEmpty(snapshot.Edges);
        Assert.Equal(snapshot.Nodes.Count, snapshot.NodeCount);
        Assert.Equal(snapshot.Edges.Count, snapshot.EdgeCount);
    }
}
