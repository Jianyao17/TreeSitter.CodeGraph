---
name: add-tree-sitter-language
description: Comprehensive step-by-step guide for adding a new programming language and its test suite to the TreeSitter.CodeGraph library. Use when adding new language support, Tree-Sitter grammars, AST queries, or test fixtures. Panduan komprehensif langkah demi langkah untuk menambahkan bahasa pemrograman baru ke library TreeSitter.CodeGraph.
---

# Skill: Adding a New Programming Language / Menambahkan Bahasa Baru

## Objective / Tujuan
A step-by-step integrated guide (implementation + testing) for adding a new programming language to the `TreeSitter.CodeGraph` library. This document is **vendor-agnostic** and designed to be executed with high precision by AI coding assistants (Antigravity, Cursor, Claude Code, GitHub Copilot, etc.) as well as human developers.

*Panduan langkah demi langkah yang terintegrasi (implementasi + pengujian) untuk menambahkan bahasa baru ke library `TreeSitter.CodeGraph`. Dirancang agar dapat dieksekusi secara presisi oleh AI assistant maupun developer manusia.*

---

## 📌 MANDATORY: Read First (Canonical References)

Before writing any code, inspect the following files to understand current architectural conventions:

### 1. Data Structures & Contracts:
- `src/Domain/Languages/LanguageDefinition.cs` - Language metadata model (extensions, identifiers).
- `src/Domain/TreeSitter/LangQueryData.cs` - Records: `LangQueryResult`, `NamespaceInfo`, `ClassInfo`, `FunctionInfo`, `CallInfo`, `TypeRefInfo`, `IncludeInfo`.
- `src/Domain/TreeSitter/AnalysisLanguage.cs` - Enum of supported languages.
- `src/Domain/Graph/GraphNode.cs` & `GraphEdge.cs` - Node types & relationship edges.
- `src/Languages/LanguageRegistry.cs` - Single Source of Truth for language catalog.

### 2. Base Class & Engine:
- `src/TreeSitter/BaseLangQuery.cs` - Abstract base class (`ExtractAll()`, Template Method Pattern).
- `src/TreeSitter/TreeSitterAnalyzer.cs` - Two-pass relation analyzer (Pass 1: Declarations, Pass 2: Usages).

### 3. Canonical Reference Implementations & Tests:
- **C#** (Namespace-based language):
  - `src/TreeSitter/LangAnalyzer/CSharpLangQuery.cs`
  - `src/TreeSitter/QueryDefinitions/CSharpQueries.cs`
  - `test/LangQuery/CSharpLangQueryTests.cs`
- **JavaScript** (Directory/folder hierarchy-based language):
  - `src/TreeSitter/LangAnalyzer/JavaScriptLangQuery.cs`
  - `src/TreeSitter/QueryDefinitions/JavaScriptQueries.cs`
  - `test/LangQuery/JavaScriptLangQueryTests.cs`

---

## File Modification Checklist (9 Steps / 9 Langkah)

| # | Action | File Location | Description / Deskripsi |
|---|--------|---------------|-------------------------|
| **1** | NEW | `test/Fixtures/{Language}/...` | Minimum 2 realistic source code fixture files |
| **2** | MODIFY | `src/Domain/TreeSitter/AnalysisLanguage.cs` | Add new language enum member |
| **3** | MODIFY | `src/Languages/LanguageRegistry.cs` | Register language metadata (name, extensions, Tree-Sitter grammar ID) |
| **4** | NEW | `src/TreeSitter/QueryDefinitions/{Language}Queries.cs` | S-expression Tree-Sitter AST queries |
| **5** | NEW | `src/TreeSitter/LangAnalyzer/{Language}LangQuery.cs` | Language-specific extractor deriving from `BaseLangQuery` |
| **6** | MODIFY | `src/TreeSitter/TreeSitterAnalyzer.cs` | Add factory case in `CreateLangQuery()` |
| **7** | NEW | `test/LangQuery/{Language}LangQueryTests.cs` | Unit test query extraction (8-10 tests) |
| **8** | MODIFY | `test/Utils/ParserPoolTests.cs` | Verify Tree-Sitter parser loads grammar |
| **9** | MODIFY | `test/Analyzer/TreeSitterAnalyzerTests.cs` | End-to-end two-pass graph integration test |

---

## Step-by-Step Execution Guide

### Step 1: Create Fixture Files / Buat File Fixture
Location: `test/Fixtures/{Language}/`  
Create at least 2 realistic, syntactically valid source files covering:
- Class, struct, interface, or module declarations
- Inter-file method or function invocations
- Import / include statements
- Typed parameters or return types (if supported by language)

### Step 2: Add Enum Value / Tambahkan Enum
File: `src/Domain/TreeSitter/AnalysisLanguage.cs`
```csharp
public enum AnalysisLanguage
{
    CSharp,
    JavaScript,
    Php,
    Cpp,
    NewLanguage // Add your new language here
}
```

### Step 3: Register in LanguageRegistry / Daftarkan di LanguageRegistry
File: `src/Languages/LanguageRegistry.cs`  
Add entry in `GetDefaultDefinitions()`:
```csharp
new LanguageDefinition(
    "New Language",
    AnalysisLanguage.NewLanguage,
    new[] { ".ext1", ".ext2" },
    "tree-sitter-id")
```
*Note: `tree-sitter-id` is lowercase and must match the grammar name in `TreeSitter.DotNet` (e.g. `"python"`, `"ruby"`, `"go"`, `"rust"`).*

### Step 4: Define Tree-Sitter S-Expression Queries
File: `src/TreeSitter/QueryDefinitions/{Language}Queries.cs`  
Define query constants for capturing AST nodes:
- `NamespaceQuery` (optional if language is directory-based)
- `ClassQuery` (captures `@class.def`, `@class.name`)
- `FunctionQuery` (captures `@func.def`, `@func.name`, `@func.params`)
- `CallQuery` (captures `@call`, `@call.name`, `@call.object`)
- `TypeRefQuery` (captures `@type.ref`)
- `IncludeQuery` (captures `@include`, `@include.path`)

### Step 5: Implement LangQuery Extractor
File: `src/TreeSitter/LangAnalyzer/{Language}LangQuery.cs`  
Inherit from `BaseLangQuery`:
```csharp
public sealed class NewLanguageLangQuery : BaseLangQuery
{
    public NewLanguageLangQuery() : base(AnalysisLanguage.NewLanguage) { }

    public override bool UsesNamespace => true; // Set false if language is directory-based

    protected override List<NamespaceInfo> QueryNamespaces(Node root, Language lang) { ... }
    protected override List<ClassInfo> QueryClasses(Node root, Language lang) { ... }
    protected override List<FunctionInfo> QueryFunctions(Node root, Language lang) { ... }
    protected override List<CallInfo> QueryCalls(Node root, Language lang) { ... }
    protected override List<TypeRefInfo> QueryTypeRefs(Node root, Language lang) { ... }
    protected override List<IncludeInfo> QueryIncludes(Node root, Language lang) { ... }
}
```

### Step 6: Register in TreeSitterAnalyzer
File: `src/TreeSitter/TreeSitterAnalyzer.cs`  
Add factory mapping inside `CreateLangQuery()`:
```csharp
AnalysisLanguage.NewLanguage => new NewLanguageLangQuery(),
```

### Step 7: Write Test Suite / Tulis Test Suite
1. `test/LangQuery/{Language}LangQueryTests.cs`:
   - Test class, method, function, call, type ref, and include extraction from fixtures.
2. `test/Utils/ParserPoolTests.cs`:
   - Add test case `Parse_{Language}_TreeHasCorrectStructure`.
3. `test/Analyzer/TreeSitterAnalyzerTests.cs`:
   - Add end-to-end integration test verifying that `CodeGraph` generates `Nodes`, `SourceRelEdges` (`BelongsTo`), and `UseRelEdges` (`Call`).

---

## Verification & Quality Gate / Verifikasi

Run test suite via terminal:
```bash
# Run tests for specific language
dotnet test --filter "FullyQualifiedName~{Language}"

# Run complete test suite across all target frameworks (.NET 8, 9, 10)
dotnet test
```

**Quality Gate Criteria:**
- 0 Warnings, 0 Errors
- 100% of unit & integration tests PASS
