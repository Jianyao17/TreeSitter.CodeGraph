using TreeSitter;
using GithubAnalyzer.Analysis.Domain.TreeSitter;
using GithubAnalyzer.Analysis.TreeSitter.QueryDefinitions;

namespace GithubAnalyzer.Analysis.TreeSitter.LangAnalyzer;

/// <summary>
/// Implementasi query tree-sitter untuk bahasa PHP.
/// Menggunakan namespace sebagai hierarchy utama.
/// Mendukung include/require.
/// </summary>
public sealed class PhpLangQuery : BaseLangQuery
{
    public PhpLangQuery() : base(AnalysisLanguage.Php) { }

    public override bool UsesNamespace => true;

    protected override List<NamespaceInfo> QueryNamespaces(Node root, Language lang)
    {
        var result = new List<NamespaceInfo>();

        foreach (var match in RunQuery(PhpQueries.Namespace, root, lang))
        {
            var node = GetCaptureNode(match, "ns_name");
            if (node is null) continue;

            // PHP namespace menggunakan backslash, normalisasi ke dot
            var nsName = node.Text.Replace("\\", ".");

            result.Add(new NamespaceInfo(
                Name: nsName,
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

        foreach (var match in RunQuery(PhpQueries.Class, root, lang))
        {
            var node = GetCaptureNode(match, "class_name")
                    ?? GetCaptureNode(match, "iface_name")
                    ?? GetCaptureNode(match, "trait_name");
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

        foreach (var match in RunQuery(PhpQueries.Function, root, lang))
        {
            var nameNode = GetCaptureNode(match, "fn_name");
            var paramsNode = GetCaptureNode(match, "params");
            if (nameNode is null) continue;

            var line = nameNode.StartPosition.Row;

            // Extract parameter types (PHP punya optional type hints)
            var paramTypes = paramsNode is not null
                ? ExtractParamTypes(PhpQueries.ParameterType, paramsNode, lang)
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

        foreach (var match in RunQuery(PhpQueries.FunctionCall, root, lang))
        {
            var node = GetCaptureNode(match, "call_name");
            if (node is null) continue;

            // Deteksi object name dari member_call_expression
            string? objectName = null;
            var parent = node.Parent;
            if (parent?.Type == "member_call_expression")
            {
                var objNode = parent.GetChildForField("object");
                if (objNode is not null)
                    objectName = objNode.Text;
            }

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

        foreach (var capture in RunQueryCaptures(PhpQueries.TypeReference, root, lang))
        {
            if (capture.Name != "type_ref") continue;

            result.Add(new TypeRefInfo(
                Name: capture.Node.Text,
                Line: capture.Node.StartPosition.Row
            ));
        }

        return result;
    }

    protected override List<IncludeInfo> QueryIncludes(Node root, Language lang)
    {
        var result = new List<IncludeInfo>();

        foreach (var capture in RunQueryCaptures(PhpQueries.Include, root, lang))
        {
            if (capture.Name != "include_path") continue;

            // Bersihkan kutip dari path
            var path = capture.Node.Text.Trim('\'', '"');

            result.Add(new IncludeInfo(
                Path: path,
                Line: capture.Node.StartPosition.Row
            ));
        }

        return result;
    }
}
