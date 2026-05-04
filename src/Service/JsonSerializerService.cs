using System.Text.Json;
using GithubAnalyzer.Analysis.Domain.Graph;

namespace GithubAnalyzer.Analysis.Service;

public class JsonSerializerService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string Serialize(CodeGraph graph)
    {
        return JsonSerializer.Serialize(graph, Options);
    }
}