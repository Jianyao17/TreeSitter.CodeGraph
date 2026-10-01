---
name: add-tree-sitter-language
description: Panduan komprehensif langkah demi langkah untuk menambahkan dukungan bahasa pemrograman baru ke library TreeSitter.CodeGraph beserta seluruh test suite-nya. Gunakan saat menambahkan bahasa baru, grammar tree-sitter, AST query, atau test fixtures.
---

# Skill: Menambahkan Dukungan Bahasa Pemrograman Baru

## Tujuan
Panduan langkah demi langkah yang terintegrasi (implementasi + pengujian) untuk menambahkan bahasa pemrograman baru ke library `TreeSitter.CodeGraph`. Dokumen ini dirancang **vendor-agnostic** sehingga dapat dieksekusi secara presisi oleh AI coding assistant (Antigravity, Cursor, Claude Code, Copilot, dll) maupun developer manusia.

---

## ⚠️ WAJIB: Baca Terlebih Dahulu (Canonical References)

Sebelum menulis kode apapun, baca file-file berikut untuk memahami konvensi terkini:

### 1. Struktur Data & Kontrak:
- `src/Domain/Languages/LanguageDefinition.cs` — model metadata bahasa
- `src/Domain/TreeSitter/LangQueryData.cs` — records: `LangQueryResult`, `NamespaceInfo`, `ClassInfo`, `FunctionInfo`, `CallInfo`, `TypeRefInfo`, `IncludeInfo`
- `src/Domain/TreeSitter/AnalysisLanguage.cs` — enum bahasa yang didukung
- `src/Domain/Graph/GraphNode.cs` & `GraphEdge.cs` — tipe node & relasi
- `src/Languages/LanguageRegistry.cs` — Single Source of Truth untuk katalog bahasa

### 2. Base Class & Engine:
- `src/TreeSitter/BaseLangQuery.cs` — abstract base class (API: `ExtractAll()`, Template Method Pattern)
- `src/TreeSitter/TreeSitterAnalyzer.cs` — two-pass relation analyzer (Pass 1: Declarations, Pass 2: Usages)

### 3. Contoh Referensi Implementasi & Test yang Sudah Ada:
- C# (bahasa dengan namespace):
  - `src/TreeSitter/LangAnalyzer/CSharpLangQuery.cs`
  - `src/TreeSitter/QueryDefinitions/CSharpQueries.cs`
  - `test/LangQuery/CSharpLangQueryTests.cs`
- JavaScript (bahasa directory/folder hierarchy):
  - `src/TreeSitter/LangAnalyzer/JavaScriptLangQuery.cs`
  - `src/TreeSitter/QueryDefinitions/JavaScriptQueries.cs`
  - `test/LangQuery/JavaScriptLangQueryTests.cs`

---

## Checklist File yang Disentuh (Total: 6 Langkah)

| # | Aksi | Lokasi File | Deskripsi |
|---|------|-------------|-----------|
| **1** | NEW | `test/Fixtures/{Bahasa}/...` | Minimal 2 file source code valid & realistis |
| **2** | MODIFY | `src/Domain/TreeSitter/AnalysisLanguage.cs` | Tambahkan nilai enum bahasa baru |
| **3** | MODIFY | `src/Languages/LanguageRegistry.cs` | Daftarkan metadata bahasa (nama, ekstensi, Tree-Sitter ID) |
| **4** | NEW | `src/TreeSitter/QueryDefinitions/{Bahasa}Queries.cs` | S-expression Tree-Sitter queries |
| **5** | NEW | `src/TreeSitter/LangAnalyzer/{Bahasa}LangQuery.cs` | Extractor khusus turunan `BaseLangQuery` |
| **6** | MODIFY | `src/TreeSitter/TreeSitterAnalyzer.cs` | Tambahkan case di `CreateLangQuery()` |
| **7** | NEW | `test/LangQuery/{Bahasa}LangQueryTests.cs` | Unit test query extraction (8-10 tests) |
| **8** | MODIFY | `test/Utils/ParserPoolTests.cs` | Unit test grammar parsing Tree-Sitter |
| **9** | MODIFY | `test/Analyzer/TreeSitterAnalyzerTests.cs` | Test integrasi graf dua-fase |

---

## Langkah-Langkah Eksekusi

### Langkah 1: Buat Fixture Files
Lokasi: `test/Fixtures/{Bahasa}/`
Buat minimal 2 file kode sumber yang valid dan realistis. Harus mencakup:
- Deklarasi class / struct / module
- Pemanggilan method / fungsi antar-file
- Include / import jika didukung bahasa
- Parameter dengan type annotation (jika didukung)

### Langkah 2: Tambahkan Enum Value
File: `src/Domain/TreeSitter/AnalysisLanguage.cs`
```csharp
public enum AnalysisLanguage
{
    CSharp,
    JavaScript,
    Php,
    Cpp,
    NamaBahasaBaru // ← Tambahkan di sini
}
```

### Langkah 3: Daftarkan di LanguageRegistry
File: `src/Languages/LanguageRegistry.cs`
Tambahkan entri baru di method `GetDefaultDefinitions()`:
```csharp
new LanguageDefinition(
    "Nama Bahasa",
    AnalysisLanguage.NamaBahasaBaru,
    new[] { ".ext1", ".ext2" },
    "tree-sitter-id")
```
*Catatan: `tree-sitter-id` biasanya lowercase (contoh: `"python"`, `"ruby"`, `"go"`, `"rust"`).*

### Langkah 4: Buat Query Definitions
File: `src/TreeSitter/QueryDefinitions/{Bahasa}Queries.cs`
Buat konstanta query S-expression terpisah untuk setiap capture:
- `NamespaceQuery` (opsional jika bahasa tidak memiliki namespace)
- `ClassQuery` (capture `@class.def` dan `@class.name`)
- `FunctionQuery` (capture `@func.def`, `@func.name`, `@func.params`)
- `CallQuery` (capture `@call`, `@call.name`, `@call.object`)
- `TypeRefQuery` (capture `@type.ref`)
- `IncludeQuery` (capture `@include`, `@include.path`)

### Langkah 5: Implementasikan LangQuery
File: `src/TreeSitter/LangAnalyzer/{Bahasa}LangQuery.cs`
Turunkan dari `BaseLangQuery`:
```csharp
public sealed class NamaBahasaLangQuery : BaseLangQuery
{
    public NamaBahasaLangQuery() : base(AnalysisLanguage.NamaBahasaBaru) { }

    public override bool UsesNamespace => true; // atau false jika directory-based

    protected override List<NamespaceInfo> QueryNamespaces(Node root, Language lang) { ... }
    protected override List<ClassInfo> QueryClasses(Node root, Language lang) { ... }
    protected override List<FunctionInfo> QueryFunctions(Node root, Language lang) { ... }
    protected override List<CallInfo> QueryCalls(Node root, Language lang) { ... }
    protected override List<TypeRefInfo> QueryTypeRefs(Node root, Language lang) { ... }
    protected override List<IncludeInfo> QueryIncludes(Node root, Language lang) { ... }
}
```

### Langkah 6: Hubungkan ke Analyzer
File: `src/TreeSitter/TreeSitterAnalyzer.cs`
Tambahkan case baru di `CreateLangQuery()`:
```csharp
AnalysisLanguage.NamaBahasaBaru => new NamaBahasaLangQuery(),
```

### Langkah 7: Tulis Test Suite & Quality Gate
1. `test/LangQuery/{Bahasa}LangQueryTests.cs`:
   - Uji ekstraksi class, method, function, call, type ref, dan include dari file fixture.
2. `test/Utils/ParserPoolTests.cs`:
   - Tambahkan test `Parse_{Bahasa}_TreeHasCorrectStructure`.
3. `test/Analyzer/TreeSitterAnalyzerTests.cs`:
   - Tambahkan test integrasi end-to-end memastikan `CodeGraph` menghasilkan `Nodes`, `SourceRelEdges`, dan `UseRelEdges` (Call).

---

## Verifikasi Quality Gate

Jalankan pengujian via terminal:
```bash
dotnet test --filter "FullyQualifiedName~{Bahasa}"
dotnet test
```
**Kriteria Lolos:** 0 Warning, 0 Error, seluruh test PASS 100%.
