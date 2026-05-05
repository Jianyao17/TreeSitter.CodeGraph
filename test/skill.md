# Skill: Menambahkan Test Suite untuk Bahasa Pemrograman Baru

## Tujuan

Panduan langkah demi langkah untuk menambahkan test suite lengkap saat bahasa pemrograman baru ditambahkan ke modul analisis `Github-Analyzer.Analysis`.

Dokumen ini adalah **pasangan** dari `src/Github-Analyzer.Analysis/skill.md` yang mengatur implementasi bahasa baru. Skill ini mengatur **pengujiannya**.

---

## ⚠️ WAJIB: Baca Terlebih Dahulu

**Sebelum menulis test apapun, WAJIB baca file-file berikut:**

### File implementasi yang diuji:
1. `src/Github-Analyzer.Analysis/TreeSitter/LangAnalyzer/{Bahasa}LangQuery.cs` — implementasi LangQuery bahasa baru
2. `src/Github-Analyzer.Analysis/TreeSitter/QueryDefinitions/{Bahasa}Queries.cs` — S-expression queries
3. `src/Github-Analyzer.Analysis/TreeSitter/BaseLangQuery.cs` — base class (API: `ExtractAll()`)
4. `src/Github-Analyzer.Analysis/Domain/TreeSitter/LangQueryData.cs` — output types: `LangQueryResult`, `NamespaceInfo`, `ClassInfo`, `FunctionInfo`, `CallInfo`, `TypeRefInfo`, `IncludeInfo`
5. `src/Github-Analyzer.Analysis/Domain/TreeSitter/AnalysisLanguage.cs` — enum bahasa

### File test yang sudah ada (sebagai referensi pola):
1. `test/Github-Analyzer.Analysis.Tests/LangQuery/CSharpLangQueryTests.cs` — contoh bahasa dengan namespace
2. `test/Github-Analyzer.Analysis.Tests/LangQuery/JavaScriptLangQueryTests.cs` — contoh bahasa tanpa namespace
3. `test/Github-Analyzer.Analysis.Tests/Utils/ParserPoolTests.cs` — referensi validasi tree structure
4. `test/Github-Analyzer.Analysis.Tests/Analyzer/TreeSitterAnalyzerTests.cs` — referensi test integrasi

**Jika ada perbedaan antara panduan ini dan kode aktual, SELALU ikuti kode aktual.**

---

## Checklist File yang Harus Dibuat/Diubah

Untuk setiap bahasa baru, sentuh **tepat 4 file** (2 baru, 2 modifikasi):

| # | Aksi | File | Deskripsi |
|---|------|------|-----------|
| 1 | NEW | `Fixtures/{Bahasa}/...` | Minimal 2 file fixture realistis |
| 2 | NEW | `LangQuery/{Bahasa}LangQueryTests.cs` | 8–10 test untuk LangQuery |
| 3 | MODIFY | `Utils/ParserPoolTests.cs` | Tambah test `Parse_{Bahasa}_TreeHasCorrectStructure` |
| 4 | MODIFY | `Analyzer/TreeSitterAnalyzerTests.cs` | Tambah 1–2 test integrasi untuk bahasa baru |

---

## Langkah-Langkah

### Langkah 1: Buat Fixture Files

**Lokasi:** `Fixtures/{Bahasa}/`

Buat minimal 2 file source code yang valid dan realistis. Fixture harus mencakup:

- **Deklarasi class** dengan beberapa method
- **Pemanggilan method** antar-class (agar Pass 2 usage scanning bisa tervalidasi)
- **Import/include** jika bahasa mendukung (agar edge `Include` bisa diuji)
- **Namespace/package** jika bahasa mendukung
- **Type annotation di parameter** jika bahasa mendukung

**Aturan fixture:**
- Harus **valid secara sintaks** — bisa di-parse Tree-Sitter tanpa error
- Line ending **LF** (Unix-style), encoding **UTF-8 tanpa BOM**
- Tidak perlu bisa di-compile/run — cukup bisa di-parse

**Contoh struktur fixture** untuk bahasa dengan namespace (misal Python):

```
Fixtures/Python/
├── controllers/
│   └── user_controller.py
└── services/
    └── user_service.py
```

**Penting:** File fixture `.cs` (jika menambahkan fixture C#) otomatis di-exclude dari kompilasi oleh rule `<Compile Remove="Fixtures\**\*.cs" />` di `.csproj`. Semua file di `Fixtures/` di-copy ke output via `<Content Include="Fixtures\**\*" CopyToOutputDirectory="PreserveNewest" />`.

Jika file fixture menggunakan ekstensi baru yang belum ada, **tidak perlu** mengubah `.csproj` — semua file di `Fixtures/` sudah di-copy otomatis.

### Langkah 2: Buat LangQuery Test Class

**File baru:** `LangQuery/{Bahasa}LangQueryTests.cs`

#### Template

```csharp
using GithubAnalyzer.Analysis.TreeSitter.LangAnalyzer;

namespace GithubAnalyzer.Analysis.Tests.LangQuery;

/// <summary>
/// Menguji ekstraksi deklarasi dan usage dari kode {NamaBahasa}.
/// Contoh: {contoh_skenario_utama}.
/// </summary>
public class {Bahasa}LangQueryTests
{
    // Source code sample di-define sebagai inline string constant.
    // Harus cukup kaya: minimal 2 class, beberapa method,
    // ada pemanggilan antar-class, ada `new X()`.
    private const string SampleCode = @"...";

    // === Declaration Tests (Pass 1) ===

    [Fact]
    public void QueryNamespaces_{Skenario}()
    {
        try
        {
            using var query = new {Bahasa}LangQuery();
            var result = query.ExtractAll(SampleCode);
            // Assert namespace terdeteksi / empty list untuk bahasa tanpa namespace
        }
        catch (DllNotFoundException)
        {
            Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
        }
    }

    [Fact]
    public void QueryClasses_DetectsClasses() { /* ... */ }

    [Fact]
    public void QueryFunctions_DetectsMethodsWithParams() { /* ... */ }

    // === Usage Tests (Pass 2) ===

    [Fact]
    public void QueryCalls_DetectsCalls() { /* ... */ }

    [Fact]
    public void QueryTypeRefs_DetectsNew() { /* ... */ }

    [Fact]
    public void QueryIncludes_DetectsImport() { /* ... (jika berlaku) */ }

    // === Format & Edge Case ===

    [Fact]
    public void ExtractAll_CRLF_SameDeclarationCount() { /* ... */ }

    [Fact]
    public void ExtractAll_EmptySource_ReturnsEmptyLists() { /* ... */ }
}
```

#### Test wajib per LangQuery (minimal 8 test):

| # | Test Name Pattern | Yang Diuji |
|---|------------------|------------|
| 1 | `QueryNamespaces_{Skenario}` | Namespace terdeteksi (atau empty list jika tidak punya) |
| 2 | `QueryClasses_DetectsClasses` | Class name, `ParentNamespace`, `EndLine > StartLine` |
| 3 | `QueryFunctions_DetectsMethodsWithParams` | Function name, `ParentClass`, `Params` |
| 4 | `QueryCalls_DetectsCalls` | Call name, `ObjectName` |
| 5 | `QueryTypeRefs_DetectsNew` | `new X()` → name `X` terdeteksi |
| 6 | `QueryIncludes_DetectsImport` | Path import/include tanpa quote/bracket |
| 7 | `ExtractAll_CRLF_SameDeclarationCount` | LF vs CRLF → class/function count sama |
| 8 | `ExtractAll_EmptySource_ReturnsEmptyLists` | Source kosong → semua list kosong, tidak throw |

#### Checklist validasi per bahasa:

| Karakteristik | Yang Perlu Diuji |
|---------------|-----------------|
| **Punya namespace** (C#, PHP, C++, Java, Go) | `ParentNamespace` terisi di ClassInfo/FunctionInfo |
| **Tidak punya namespace** (JS, Python, Ruby) | `QueryNamespaces` return empty list, `ParentNamespace == null` |
| **Punya type annotation** (C#, PHP, C++, Java, Go) | `Params` berisi type string: `"int"`, `"int,string"` |
| **Tanpa type annotation** (JS, Python, Ruby) | `Params == ""` |
| **Punya import/include** (JS, PHP, C++, Python, Go, Java) | `QueryIncludes` / override `QueryIncludes` return non-empty |
| **Normalisasi separator** (PHP `\` → `.`, C++ `::` → `.`) | Namespace name sudah di-normalize |
| **Qualified name splitting** (C++ `Class::Method`) | `FunctionInfo.Name` hanya berisi nama method |

### Langkah 3: Update ParserPoolTests

**File:** `Utils/ParserPoolTests.cs`

Tambahkan satu test method baru:

```csharp
[Fact]
public void Parse_{Bahasa}_TreeHasCorrectStructure()
{
    try
    {
        using var pool = new ParserPool(AnalysisLanguage.{Bahasa});
        var code = "{source_code_minimal}";
        using var tree = pool.Parse(code);

        var root = tree.RootNode;
        Assert.Equal("{expected_root_type}", root.Type);

        var expr = root.Expression;
        Assert.Contains("{expected_class_node_type}", expr);  // misal: "class_definition"
        Assert.Contains("{expected_function_node_type}", expr);  // misal: "function_definition"

        Assert.Equal(code, root.Text);
    }
    catch (DllNotFoundException)
    {
        Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
    }
}
```

**Cara menentukan root type dan node types:**

| Bahasa | Root Type | Class Node | Function Node |
|--------|-----------|------------|---------------|
| C# | `compilation_unit` | `class_declaration` | `method_declaration` |
| JavaScript | `program` | `class_declaration` | `method_definition` |
| PHP | `program` | `class_declaration` | `method_declaration` |
| C++ | `translation_unit` | `class_specifier` | `function_definition` |
| Python | `module` | `class_definition` | `function_definition` |
| Java | `program` | `class_declaration` | `method_declaration` |
| Go | `source_file` | `type_declaration` | `function_declaration` |
| Ruby | `program` | `class` | `method` |
| Rust | `source_file` | `struct_item` | `function_item` |

> **Tip:** Jika tidak yakin, parse contoh kode dan cetak `tree.RootNode.Expression` untuk melihat grammar node names.

Juga tambahkan enum value baru di test `Constructor_AllSupportedLanguages_Succeeds`:

```csharp
[Theory]
[InlineData(AnalysisLanguage.CSharp)]
[InlineData(AnalysisLanguage.JavaScript)]
[InlineData(AnalysisLanguage.Php)]
[InlineData(AnalysisLanguage.Cpp)]
[InlineData(AnalysisLanguage.{Bahasa})]  // ← tambahkan
public void Constructor_AllSupportedLanguages_Succeeds(AnalysisLanguage language)
```

### Langkah 4: Update TreeSitterAnalyzerTests (Integrasi)

**File:** `Analyzer/TreeSitterAnalyzerTests.cs`

Tambahkan 1–2 test integrasi yang menggunakan fixture dari Langkah 1.

#### Test wajib:

```csharp
[Fact]
public async Task AnalyzeAsync_{Bahasa}_HasNodesAndEdges()
{
    try
    {
        var (graph, progress) = await RunAnalysisAsync(
            "{Bahasa}",                     // subfolder di Fixtures/
            AnalysisLanguage.{Bahasa},
            [".{ext1}", ".{ext2}"]);        // ekstensi file

        // Progress
        Assert.True(progress[^1].IsCompleted);

        // Nodes
        Assert.Contains(graph.Nodes, n => n.Type == NodeType.File);
        Assert.Contains(graph.Nodes, n => n.Type == NodeType.Class);
        Assert.Contains(graph.Nodes, n => n.Type == NodeType.Function);

        // No duplicate PathIds
        Assert.Equal(graph.Nodes.Count, graph.Nodes.Select(n => n.PathId).Distinct().Count());

        // Source edges reference existing nodes
        var nodeIds = new HashSet<string>(graph.Nodes.Select(n => n.PathId));
        foreach (var edge in graph.SourceRelEdges)
        {
            Assert.True(nodeIds.Contains(edge.From), $"SourceRelEdge.From not found: {edge.From}");
            Assert.True(nodeIds.Contains(edge.To), $"SourceRelEdge.To not found: {edge.To}");
        }

        // Use edges reference existing nodes
        foreach (var edge in graph.UseRelEdges)
        {
            Assert.True(nodeIds.Contains(edge.From), $"UseRelEdge.From not found: {edge.From}");
            Assert.True(nodeIds.Contains(edge.To), $"UseRelEdge.To not found: {edge.To}");
        }
    }
    catch (DllNotFoundException)
    {
        Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
    }
}
```

#### Test opsional (jika bahasa punya include/import):

```csharp
[Fact]
public async Task AnalyzeAsync_{Bahasa}_HasIncludeEdges()
{
    try
    {
        var (graph, _) = await RunAnalysisAsync("{Bahasa}", AnalysisLanguage.{Bahasa}, [".{ext}"]);
        var includeEdges = graph.SourceRelEdges.Where(e => e.Type == EdgeType.Include).ToList();
        Assert.NotEmpty(includeEdges);
    }
    catch (DllNotFoundException)
    {
        Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
    }
}
```

---

## Aturan Penulisan Test

### 1. DllNotFoundException — Graceful Skip

**Setiap test** yang membutuhkan Tree-sitter native binary **WAJIB** dibungkus:

```csharp
try
{
    // ... test logic ...
}
catch (DllNotFoundException)
{
    Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
}
```

Ini memastikan test tidak gagal di CI environment yang belum punya native library.

### 2. Penamaan Test

Gunakan pola `MetodeYangDiuji_Skenario_HasilYangDiharapkan`:
- ✅ `QueryClasses_DetectsClassesWithNamespace`
- ✅ `ExtractAll_CRLF_SameDeclarationCount`
- ✅ `Parse_Python_TreeHasCorrectStructure`
- ❌ `TestClasses` (tidak deskriptif)

### 3. Source Code Sample

- Definisikan sebagai `private const string SampleCode = @"...";` di bagian atas test class
- Harus inline — **jangan baca dari file fixture** untuk LangQuery tests
- Fixture files hanya digunakan untuk **integration test** (TreeSitterAnalyzerTests)

### 4. Independensi Test

- Setiap test method **independent** — tidak ada shared mutable state
- `using var query = new {Bahasa}LangQuery();` di setiap test method
- Jangan gunakan `[ClassFixture]` atau shared state

### 5. Path Runtime

```csharp
var fixturesPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "{Bahasa}");
```

Jangan hardcode path absolut. Selalu gunakan `Path.Combine`.

### 6. XML Doc Wajib

Setiap test class harus punya `<summary>` yang menjelaskan bahasa dan contoh skenario:

```csharp
/// <summary>
/// Menguji ekstraksi deklarasi dan usage dari kode Python.
/// Contoh: class UserService → def find_by_id(id: int).
/// </summary>
```

### 7. Deterministic

- Jangan gunakan `Thread.Sleep`, `DateTime.Now`, atau random values
- Semua assertion harus deterministik

---

## Struktur Folder Test (Setelah Menambahkan Bahasa Baru)

```
test/Github-Analyzer.Analysis.Tests/
├── Fixtures/
│   ├── CSharp/          (sudah ada)
│   ├── JavaScript/      (sudah ada)
│   ├── Php/             (sudah ada)
│   ├── Cpp/             (sudah ada)
│   └── {Bahasa}/        ← BARU
│       ├── {subfolder}/
│       │   └── {file1}.{ext}
│       └── {subfolder}/
│           └── {file2}.{ext}
│
├── Reader/
│   ├── CodebaseReaderTests.cs
│   └── ContentFormatTests.cs
├── Utils/
│   ├── PathIdTests.cs
│   └── ParserPoolTests.cs        ← MODIFY: tambah 1 test
├── LangQuery/
│   ├── CSharpLangQueryTests.cs   (sudah ada)
│   ├── JavaScriptLangQueryTests.cs (sudah ada)
│   ├── PhpLangQueryTests.cs      (sudah ada)
│   ├── CppLangQueryTests.cs      (sudah ada)
│   └── {Bahasa}LangQueryTests.cs ← BARU
├── Analyzer/
│   └── TreeSitterAnalyzerTests.cs ← MODIFY: tambah 1-2 test
└── Github-Analyzer.Analysis.Tests.csproj
```

---

## Verifikasi

Setelah semua test ditulis, jalankan:

```bash
# Build
dotnet build test/Github-Analyzer.Analysis.Tests/Github-Analyzer.Analysis.Tests.csproj

# Run semua test
dotnet test test/Github-Analyzer.Analysis.Tests/Github-Analyzer.Analysis.Tests.csproj --verbosity normal

# Run hanya test bahasa baru
dotnet test --filter "FullyQualifiedName~{Bahasa}" --verbosity normal
```

**Kriteria sukses:** Semua test `Passed` atau `Skipped` (via DllNotFoundException). Tidak boleh ada `Failed`.

---

## Contoh Lengkap: Menambahkan Python

### 1. Fixtures

```
Fixtures/Python/
├── controllers/
│   └── user_controller.py
└── services/
    └── user_service.py
```

`user_service.py`:
```python
class UserService:
    def __init__(self):
        self.users = []

    def find_by_id(self, user_id: int):
        return self.users[user_id]

    def save(self, name: str, email: str):
        self.users.append({"name": name, "email": email})
```

`user_controller.py`:
```python
from services.user_service import UserService

class UserController:
    def __init__(self):
        self.service = UserService()

    def get_user(self, user_id: int):
        return self.service.find_by_id(user_id)

    def create_user(self, name: str, email: str):
        self.service.save(name, email)
```

### 2. LangQuery Test

File: `LangQuery/PythonLangQueryTests.cs`

Sample code inline, 8+ test methods covering `QueryNamespaces` (empty), `QueryClasses`, `QueryFunctions` (with `Params` dari type annotation), `QueryCalls`, `QueryTypeRefs`, `QueryIncludes`, CRLF, empty source.

### 3. ParserPoolTests

Tambah:
```csharp
[Fact]
public void Parse_Python_TreeHasCorrectStructure()
{
    // root.Type == "module"
    // expr contains "class_definition", "function_definition"
}
```

Dan update `[InlineData(AnalysisLanguage.Python)]` di `Constructor_AllSupportedLanguages_Succeeds`.

### 4. TreeSitterAnalyzerTests

Tambah:
```csharp
[Fact]
public async Task AnalyzeAsync_Python_HasNodesAndEdges()
{
    // RunAnalysisAsync("Python", AnalysisLanguage.Python, [".py"])
}
```

---

## Catatan Penting

1. **Source code di `SampleCode` harus berbeda dari fixture files.** SampleCode untuk LangQuery test, fixture files untuk integration test.
2. **PHP namespace quirk:** PHP `namespace App\Services;` (semicolon-style) — range-nya mungkin tidak mencakup child classes. Gunakan block-style `namespace App\Services { ... }` di SampleCode jika perlu menguji `ParentNamespace`.
3. **Tree-sitter `Node` API yang tersedia:** `.Type`, `.Text`, `.Expression`, `.StartPosition`, `.EndPosition`, `.Parent`, `.GetChildForField()`. **Tidak ada** `.ChildCount` — gunakan `.Expression.Length` atau cek child via `.GetChildForField()`.
4. **`root.Expression` (S-expression)** hanya berisi node type names, bukan text content. Gunakan `root.Text` untuk memvalidasi isi source code.
5. **Setiap test yang menggunakan Tree-sitter** harus handle `DllNotFoundException`. Test di `Reader/`, `Utils/PathIdTests.cs` yang tidak menyentuh Tree-sitter boleh tanpa wrapper.
