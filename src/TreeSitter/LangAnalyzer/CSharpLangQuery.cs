using TreeSitter;
using GithubAnalyzer.Analysis.Domain.TreeSitter;
using GithubAnalyzer.Analysis.TreeSitter.QueryDefinitions;

namespace GithubAnalyzer.Analysis.TreeSitter.LangAnalyzer;

/// <summary>
/// Implementasi query tree-sitter untuk bahasa C#.
/// Menggunakan namespace sebagai hierarchy utama.
/// </summary>
public sealed class CSharpLangQuery : BaseLangQuery
{
    public CSharpLangQuery() : base(AnalysisLanguage.CSharp) { }

    public override bool UsesNamespace => true;

    protected override List<NamespaceInfo> QueryNamespaces(Node root, Language lang)
    {
        var result = new List<NamespaceInfo>();

        foreach (var match in RunQuery(CSharpQueries.Namespace, root, lang))
        {
            var node = GetCaptureNode(match, "ns_name");
            if (node is null) continue;

            var isFileScoped = node.Parent?.Type == "file_scoped_namespace_declaration";
            var endLine = isFileScoped
                ? int.MaxValue
                : (node.Parent?.EndPosition.Row ?? node.EndPosition.Row);

            result.Add(new NamespaceInfo(
                Name: node.Text,
                StartLine: node.StartPosition.Row,
                EndLine: endLine
            ));
        }

        return result;
    }

    protected override List<ClassInfo> QueryClasses(Node root, Language lang)
    {
        var result = new List<ClassInfo>();
        var namespaces = QueryNamespaces(root, lang);

        foreach (var match in RunQuery(CSharpQueries.Class, root, lang))
        {
            // Cari capture dari salah satu alternatif
            var node = GetCaptureNode(match, "class_name")
                    ?? GetCaptureNode(match, "iface_name")
                    ?? GetCaptureNode(match, "struct_name")
                    ?? GetCaptureNode(match, "record_name");
            if (node is null) continue;

            var line = node.StartPosition.Row;

            result.Add(new ClassInfo(
                Name: node.Text,
                ParentChain: null,
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

        foreach (var match in RunQuery(CSharpQueries.Function, root, lang))
        {
            var nameNode = GetCaptureNode(match, "fn_name")
                        ?? GetCaptureNode(match, "ctor_name");
            var paramsNode = GetCaptureNode(match, "params");
            if (nameNode is null) continue;

            var line = nameNode.StartPosition.Row;

            // Extract parameter types
            var paramTypes = paramsNode is not null
                ? ExtractParamTypes(CSharpQueries.ParameterType, paramsNode, lang)
                : "";

            result.Add(new FunctionInfo(
                Name: nameNode.Text,
                ParentChain: null,
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

        foreach (var match in RunQuery(CSharpQueries.FunctionCall, root, lang))
        {
            var node = GetCaptureNode(match, "call_name");
            if (node is null) continue;

            // Coba deteksi object name dari member_access_expression
            string? objectName = null;
            var parent = node.Parent;
            if (parent?.Type == "member_access_expression")
            {
                var expr = parent.GetChildForField("expression");
                if (expr is not null)
                    objectName = expr.Text;
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

        foreach (var capture in RunQueryCaptures(CSharpQueries.TypeReference, root, lang))
        {
            if (capture.Name != "type_ref") continue;

            result.Add(new TypeRefInfo(
                Name: capture.Node.Text,
                Line: capture.Node.StartPosition.Row
            ));
        }

        return result;
    }
}
