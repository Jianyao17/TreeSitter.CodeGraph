# Skill: Menambahkan Dukungan Bahasa Pemrograman Baru

## Tujuan

Panduan langkah demi langkah untuk menambahkan dukungan bahasa pemrograman baru ke modul analisis kode sumber Tree-Sitter di project `Github-Analyzer.Analysis`.

Dokumen ini dirancang **vendor-agnostic** — bisa dieksekusi oleh AI coding assistant manapun (Copilot, Claude, Gemini, Cursor, dll) tanpa asumsi terhadap tool tertentu.

---

## ⚠️ WAJIB: Sinkronisasi Kode Terbaru

**Sebelum mulai menulis kode apapun, WAJIB baca file-file berikut untuk memahami struktur dan konvensi terkini:**

### File yang HARUS dibaca terlebih dahulu:

1. **Struktur data output:**
   - `Domain/TreeSitter/LangQueryData.cs` — record types `LangQueryResult`, `NamespaceInfo`, `ClassInfo`, `FunctionInfo`, `CallInfo`, `TypeRefInfo`, `IncludeInfo`
   - `Domain/TreeSitter/AnalysisLanguage.cs` — enum bahasa yang didukung
   - `Domain/Graph/GraphNode.cs` — `NodeType` enum dan format `PathId`
   - `Domain/Graph/GraphEdge.cs` — `EdgeType` enum

2. **Base class yang akan di-extend:**
   - `TreeSitter/BaseLangQuery.cs` — abstract class dengan abstract methods yang harus diimplementasi

3. **Contoh implementasi bahasa yang sudah ada (pilih salah satu sebagai referensi):**
   - `TreeSitter/LangAnalyzer/CSharpLangQuery.cs` — contoh bahasa dengan namespace
   - `TreeSitter/LangAnalyzer/JavaScriptLangQuery.cs` — contoh bahasa tanpa namespace
   - `TreeSitter/QueryDefinitions/CSharpQueries.cs` — contoh query definitions

4. **Analyzer utama yang menggunakan BaseLangQuery:**
   - `TreeSitter/TreeSitterAnalyzer.cs` — perlu ditambahkan case baru di method `CreateLangQuery()`

5. **Utility:**
   - `TreeSitter/Utils/PathId.cs` — cara membangun PathId
   - `TreeSitter/Utils/ParserPool.cs` — mapping language enum ke tree-sitter identifier

6. **Library tree-sitter yang digunakan:**
   - Package: `TreeSitter.DotNet` (NuGet)
   - Repository: https://github.com/mariusgreuel/tree-sitter-dotnet-bindings
   - API utama: `Language`, `Parser`, `Tree`, `Query`, `QueryCursor`, `QueryMatch`, `QueryCapture`, `Node`

**Jika ada perbedaan antara panduan ini dan kode aktual, SELALU ikuti kode aktual.**

---

## Langkah-Langkah Implementasi

### Langkah 1: Tentukan Karakteristik Bahasa

Jawab pertanyaan berikut untuk bahasa target:

| Pertanyaan | Contoh Jawaban |
|---|---|
| Apakah bahasa ini punya namespace/package? | C# → ya, JS → tidak |
| Apa tree-sitter language identifier-nya? | `"python"`, `"ruby"`, `"go"` |
| Apa nama grammar node untuk class/struct? | Lihat tree-sitter grammar repo |
| Apa nama grammar node untuk function/method? | Lihat tree-sitter grammar repo |
| Apakah punya type annotation di parameter? | Python → opsional, Ruby → tidak |
| Apakah punya include/import/require? | Python → `import_statement` |

**Cara menemukan nama node grammar:**
- Buka repo grammar di `https://github.com/tree-sitter/tree-sitter-{bahasa}`
- Lihat file `grammar.js` atau `src/node-types.json`
- Atau parse contoh kode dan cetak S-expression: `tree.RootNode.Expression`

### Langkah 2: Tambahkan Enum Value

**File:** `Domain/TreeSitter/AnalysisLanguage.cs`

Tambahkan value baru ke enum `AnalysisLanguage`:

```csharp
public enum AnalysisLanguage
{
    CSharp,
    JavaScript,
    Php,
    Cpp,
    NamaBahasaBaru   // ← tambahkan di sini
}
```

### Langkah 3: Update ParserPool Mapping

**File:** `TreeSitter/Utils/ParserPool.cs`

Tambahkan case baru di method `MapLanguageId()`:

```csharp
private static string MapLanguageId(AnalysisLanguage language) => language switch
{
    // ... case yang sudah ada ...
    AnalysisLanguage.NamaBahasaBaru => "identifier-tree-sitter",  // ← tambahkan
    _ => throw new ArgumentOutOfRangeException(...)
};
```

Tree-sitter identifier biasanya lowercase, contoh: `"python"`, `"ruby"`, `"go"`, `"java"`, `"rust"`.

### Langkah 4: Buat Query Definitions

**File baru:** `TreeSitter/QueryDefinitions/{Bahasa}Queries.cs`

Buat static class berisi S-expression query strings. **Setiap tipe node harus punya query terpisah.**

Template minimal:

```csharp
namespace GithubAnalyzer.Analysis.TreeSitter.QueryDefinitions;

public static class NamaBahasaQueries
{
    // === Pass 1: Declaration ===
    
    // Isi jika bahasa punya namespace/package. Kosongkan jika tidak.
    public const string Namespace = @"...";
    
    public const string Class = @"...";
    
    public const string Function = @"...";
    
    // === Pass 2: Usage ===
    
    public const string FunctionCall = @"...";
    
    public const string TypeReference = @"...";
    
    // Opsional: jika bahasa punya include/import
    public const string Import = @"...";
    
    // Opsional: jika bahasa punya type annotations
    public const string ParameterType = @"...";
}
```

**Aturan penulisan S-expression query:**
- Gunakan `@capture_name` untuk menangkap node yang diinginkan
- Capture names yang digunakan oleh sistem:
  - Namespace: `@ns_name`
  - Class: `@class_name` (atau `@iface_name`, `@struct_name`, etc.)
  - Function: `@fn_name`, `@params`
  - Calls: `@call_name`
  - Type refs: `@type_ref`
  - Includes: `@include_path` atau `@import_path`
  - Parameter types: `@param_type`
- Gunakan `[...]` untuk alternatif multiple patterns dalam satu query

### Langkah 5: Buat Language Query Implementation

**File baru:** `TreeSitter/LangAnalyzer/{Bahasa}LangQuery.cs`

Extend `BaseLangQuery` dan implementasi semua abstract methods.

Template:

```csharp
using TreeSitter;
using GithubAnalyzer.Analysis.Domain.TreeSitter;
using GithubAnalyzer.Analysis.TreeSitter.QueryDefinitions;

namespace GithubAnalyzer.Analysis.TreeSitter.LangAnalyzer;

public sealed class NamaBahasaLangQuery : BaseLangQuery
{
    public NamaBahasaLangQuery() : base(AnalysisLanguage.NamaBahasaBaru) { }

    // true jika bahasa punya namespace/package, false jika directory-based
    public override bool UsesNamespace => true; // atau false

    protected override List<NamespaceInfo> QueryNamespaces(Node root, Language lang)
    {
        // Jika bahasa tidak punya namespace, return []
        // Jika punya, gunakan RunQuery() dengan query string dari QueryDefinitions
        var result = new List<NamespaceInfo>();
        foreach (var match in RunQuery(NamaBahasaQueries.Namespace, root, lang))
        {
            var node = GetCaptureNode(match, "ns_name");
            if (node is null) continue;
            result.Add(new NamespaceInfo(
                Name: node.Text,
                StartLine: node.StartPosition.Row,
                EndLine: node.Parent?.EndPosition.Row ?? node.EndPosition.Row
            ));
        }
        return result;
    }

    protected override List<ClassInfo> QueryClasses(Node root, Language lang)
    {
        var result = new List<ClassInfo>();
        var namespaces = QueryNamespaces(root, lang);
        foreach (var match in RunQuery(NamaBahasaQueries.Class, root, lang))
        {
            var node = GetCaptureNode(match, "class_name");
            if (node is null) continue;
            var line = node.StartPosition.Row;
            result.Add(new ClassInfo(
                Name: node.Text,
                ParentNamespace: FindParentNamespace(line, namespaces),
                StartLine: line,
                EndLine: node.Parent?.EndPosition.Row ?? node.EndPosition.Row
            ));
        }
        return result;
    }

    protected override List<FunctionInfo> QueryFunctions(Node root, Language lang)
    {
        var result = new List<FunctionInfo>();
        var namespaces = QueryNamespaces(root, lang);
        var classes = QueryClasses(root, lang);
        foreach (var match in RunQuery(NamaBahasaQueries.Function, root, lang))
        {
            var nameNode = GetCaptureNode(match, "fn_name");
            var paramsNode = GetCaptureNode(match, "params");
            if (nameNode is null) continue;
            var line = nameNode.StartPosition.Row;

            // Untuk bahasa tanpa type annotation, paramTypes = ""
            var paramTypes = paramsNode is not null
                ? ExtractParamTypes(NamaBahasaQueries.ParameterType, paramsNode, lang)
                : "";

            result.Add(new FunctionInfo(
                Name: nameNode.Text,
                ParentClass: FindParentClass(line, classes),
                ParentNamespace: FindParentNamespace(line, namespaces),
                Params: paramTypes,
                StartLine: line,
                EndLine: nameNode.Parent?.EndPosition.Row ?? nameNode.EndPosition.Row
            ));
        }
        return result;
    }

    protected override List<CallInfo> QueryCalls(Node root, Language lang)
    {
        var result = new List<CallInfo>();
        foreach (var match in RunQuery(NamaBahasaQueries.FunctionCall, root, lang))
        {
            var node = GetCaptureNode(match, "call_name");
            if (node is null) continue;

            // Deteksi object name jika ada (method call pada object)
            string? objectName = null;
            // ... logika spesifik bahasa ...

            result.Add(new CallInfo(
                Name: node.Text,
                ObjectName: objectName,
                Line: node.StartPosition.Row
            ));
        }
        return result;
    }

    protected override List<TypeRefInfo> QueryTypeRefs(Node root, Language lang)
    {
        var result = new List<TypeRefInfo>();
        foreach (var capture in RunQueryCaptures(NamaBahasaQueries.TypeReference, root, lang))
        {
            if (capture.Name != "type_ref") continue;
            result.Add(new TypeRefInfo(
                Name: capture.Node.Text,
                Line: capture.Node.StartPosition.Row
            ));
        }
        return result;
    }

    // Override jika bahasa punya import/include
    protected override List<IncludeInfo> QueryIncludes(Node root, Language lang)
    {
        var result = new List<IncludeInfo>();
        foreach (var capture in RunQueryCaptures(NamaBahasaQueries.Import, root, lang))
        {
            if (capture.Name != "import_path") continue;
            result.Add(new IncludeInfo(
                Path: capture.Node.Text,
                Line: capture.Node.StartPosition.Row
            ));
        }
        return result;
    }
}
```

### Langkah 6: Daftarkan di TreeSitterAnalyzer

**File:** `TreeSitter/TreeSitterAnalyzer.cs`

Tambahkan case di method `CreateLangQuery()`:

```csharp
private static BaseLangQuery CreateLangQuery(AnalysisLanguage language) => language switch
{
    // ... case yang sudah ada ...
    AnalysisLanguage.NamaBahasaBaru => new NamaBahasaLangQuery(),  // ← tambahkan
    _ => throw new ArgumentOutOfRangeException(nameof(language))
};
```

### Langkah 7: Verifikasi

1. **Build:** `dotnet build Github-Analyzer.Analysis.csproj` — harus 0 error
2. **Validasi query:** Parse contoh kode bahasa target dan cetak `tree.RootNode.Expression` untuk memverifikasi nama node grammar yang digunakan di queries benar
3. **Test manual:** Jalankan `AnalyzeAsync()` dengan file contoh bahasa baru dan periksa output `CodeGraph`

---

## Checklist File yang Harus Diubah/Dibuat

Untuk setiap bahasa baru, sentuh **tepat 5 file** (3 baru, 2 modifikasi):

| # | Aksi | File | Deskripsi |
|---|------|------|-----------|
| 1 | MODIFY | `Domain/TreeSitter/AnalysisLanguage.cs` | Tambah enum value |
| 2 | MODIFY | `TreeSitter/Utils/ParserPool.cs` | Tambah mapping ke tree-sitter ID |
| 3 | NEW | `TreeSitter/QueryDefinitions/{Bahasa}Queries.cs` | S-expression queries |
| 4 | NEW | `TreeSitter/LangAnalyzer/{Bahasa}LangQuery.cs` | Implementasi BaseLangQuery |
| 5 | MODIFY | `TreeSitter/TreeSitterAnalyzer.cs` | Tambah case di CreateLangQuery() |

---

## Konvensi Penamaan

- Enum value: PascalCase, contoh: `Python`, `Ruby`, `Go`, `Java`
- Query class: `{Bahasa}Queries`, contoh: `PythonQueries`
- LangQuery class: `{Bahasa}LangQuery`, contoh: `PythonLangQuery`
- Tree-sitter ID: lowercase dengan dash, contoh: `"python"`, `"ruby"`, `"go"`

---

## Helper Methods yang Tersedia di BaseLangQuery

| Method | Fungsi |
|--------|--------|
| `RunQuery(queryStr, root, lang)` | Jalankan S-expression query, return `List<QueryMatch>` |
| `RunQueryCaptures(queryStr, root, lang)` | Jalankan query, return `List<QueryCapture>` (lebih ringkas) |
| `GetCapture(match, "name")` | Ambil teks dari capture bernama tertentu |
| `GetCaptureNode(match, "name")` | Ambil Node dari capture bernama tertentu |
| `FindParentNamespace(line, namespaces)` | Cari namespace terdalam yang mengandung baris |
| `FindParentClass(line, classes)` | Cari class terdalam yang mengandung baris |
| `ExtractParamTypes(queryStr, paramsNode, lang)` | Extract comma-separated parameter types |

---

## Catatan Penting

1. **Tree-sitter grammar berbeda untuk setiap bahasa.** Selalu verifikasi nama node dari grammar resmi.
2. **Bahasa tanpa namespace** (JS, Python, Ruby, Go): set `UsesNamespace => false`, return `[]` dari `QueryNamespaces()`. Hierarchy akan menggunakan directory.
3. **Bahasa tanpa type annotation** (JS, Python, Ruby): gunakan `Params: ""` sehingga PathId function menjadi `functionName()`.
4. **Normalisasi simbol bahasa**: PHP `\` → `.`, C++ `::` → `.` untuk konsistensi PathId.
5. **Scope resolution** di Pass 2 sudah ditangani oleh `TreeSitterAnalyzer` — tidak perlu diimplementasi di LangQuery.
6. **Pastikan tree-sitter native library tersedia** untuk bahasa target. Cek daftar di README TreeSitter.DotNet.
7. **NodeType**: Gunakan `NodeType.Directory` untuk folder dan `NodeType.Namespace` untuk namespace/package.
8. **PathId format**: Separator `::` tersedia sebagai `PathId.Separator`. Format: Directory=`path::`, File=`path::`, Namespace=`::name`, Symbol=`path::symbol`.
9. **Internal record**: Deklarasi yang ditemukan di-track sebagai `SymbolDeclaration` (bukan `DeclaredSymbol`).
