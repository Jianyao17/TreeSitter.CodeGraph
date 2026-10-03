# TreeSitter.CodeGraph

[![Build & Test](https://github.com/Jianyao17/TreeSitter.CodeGraph/actions/workflows/ci.yml/badge.svg)](https://github.com/Jianyao17/TreeSitter.CodeGraph/actions)
[![NuGet Version](https://img.shields.io/nuget/v/TreeSitter.CodeGraph.svg?logo=nuget)](https://www.nuget.org/packages/TreeSitter.CodeGraph/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

A high-performance, multi-language static code analysis engine for .NET powered by Tree-Sitter. Generates structured code dependency graphs (`CodeGraph`) via two-phase relation analysis (*Declaration Mapping* & *Usage Scanning*).

## Features
- **Multi-language Support:** C#, JavaScript / TypeScript, PHP, C++
- **Two-Pass Graph Generation:** Hierarchical source relations (`BelongsTo`, `Define`) and usage relations (`Call`, `Include`)
- **Fast Codebase Scanner:** Flexible local filesystem reader with folder exclusions, extension filtering, and size caps
- **Automatic Language Detection:** Built-in multi-file language detection engine (`ILanguageDetector`)
- **Streaming Progress:** Real-time progress updates via `IAsyncEnumerable<TreeSitterProgress<CodeGraph>>`
- **Multi-target Framework:** Compatible with .NET 8.0 (LTS), .NET 9.0, and .NET 10.0

## Installation
```bash
dotnet add package TreeSitter.CodeGraph
```

## Quick Start
```csharp
using TreeSitter.CodeGraph.Domain.Reader;
using TreeSitter.CodeGraph.Domain.TreeSitter;
using TreeSitter.CodeGraph.Languages;
using TreeSitter.CodeGraph.Reader;
using TreeSitter.CodeGraph.TreeSitter;

// 1. Detect language
var detector = new LanguageDetector();
var detection = detector.Detect("./my-project");
var language = detection.PrimaryLanguage ?? AnalysisLanguage.CSharp;

// 2. Read codebase snapshot
var reader = new CodebaseReader();
var snapshot = await reader.ReadAsync("./my-project", new CodebaseReadOptions
{
    AllowedExtensions = language.GetSupportedExtensions()
});

// 3. Analyze code graph with streaming progress
using var analyzer = new TreeSitterAnalyzer();
await foreach (var progress in analyzer.AnalyzeAsync(snapshot, language))
{
    Console.WriteLine($"[{progress.Percentage}%] {progress.Message}");
    if (progress.IsCompleted && progress.Result != null)
    {
        var graph = progress.Result;
        Console.WriteLine($"Generated {graph.Nodes.Count} nodes and {graph.UseRelEdges.Count} call relations.");
    }
}
```

## Contributing
To add a new programming language, see our AI agent skill guide at [.agents/skills/add-language/SKILL.md](.agents/skills/add-language/SKILL.md).

## License
MIT License.
