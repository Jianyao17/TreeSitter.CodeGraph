namespace GithubAnalyzer.Analysis.Domain.TreeSitter;

/// <summary>
/// Hasil query per file dari BaseLangQuery — format standar lintas bahasa.
/// </summary>
public sealed record LangQueryResult
{
    public List<NamespaceInfo>  Namespaces  { get; init; } = [];
    public List<ClassInfo>      Classes     { get; init; } = [];
    public List<FunctionInfo>   Functions   { get; init; } = [];
    public List<CallInfo>       Calls       { get; init; } = [];
    public List<TypeRefInfo>    TypeRefs    { get; init; } = [];
    public List<IncludeInfo>    Includes    { get; init; } = [];
}

/// <summary>
/// Deklarasi namespace/package yang ditemukan.
/// </summary>
public sealed record NamespaceInfo(string Name, int StartLine, int EndLine);

/// <summary>
/// Deklarasi class/interface yang ditemukan.
/// ParentChain berisi dot-chain parent containers (class/function), e.g. "Outer.Inner".
/// </summary>
public sealed record ClassInfo(string Name, string? ParentChain, string? ParentNamespace, int StartLine, int EndLine);

/// <summary>
/// Deklarasi function/method yang ditemukan.
/// ParentChain berisi dot-chain parent containers (class/function), e.g. "Outer.Inner" atau "Outer.Method()".
/// </summary>
public sealed record FunctionInfo(string Name, string? ParentChain, string? ParentNamespace, string Params, int StartLine, int EndLine);

/// <summary>
/// Pemanggilan function yang ditemukan (usage).
/// </summary>
public sealed record CallInfo(string Name, string? ObjectName, int Line);

/// <summary>
/// Referensi ke tipe/class yang ditemukan (usage).
/// </summary>
public sealed record TypeRefInfo(string Name, int Line);

/// <summary>
/// Include/import/require yang ditemukan.
/// </summary>
public sealed record IncludeInfo(string Path, int Line);
