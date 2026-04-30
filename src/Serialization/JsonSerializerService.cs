using System.Text.Json;
using GithubAnalyzer.Analysis.Domain;

namespace GithubAnalyzer.Analysis.Serialization;

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
