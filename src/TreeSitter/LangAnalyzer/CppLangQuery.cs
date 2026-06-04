using TreeSitter;
using GithubAnalyzer.Analysis.Domain.TreeSitter;
using GithubAnalyzer.Analysis.TreeSitter.QueryDefinitions;

namespace GithubAnalyzer.Analysis.TreeSitter.LangAnalyzer;

/// <summary>
/// Implementasi query tree-sitter untuk bahasa C++.
/// Mendukung namespace (opsional) dan #include.
/// </summary>
public sealed class CppLangQuery : BaseLangQuery
{
    public CppLangQuery() : base(AnalysisLanguage.Cpp) { }

    public override bool UsesNamespace => true;

    protected override List<NamespaceInfo> QueryNamespaces(Node root, Language lang)
    {
        var result = new List<NamespaceInfo>();

        foreach (var match in RunQuery(CppQueries.Namespace, root, lang))
        {
            var node = GetCaptureNode(match, "ns_name");
            if (node is null) continue;

            // C++ namespace bisa nested: ns1::ns2 → normalisasi ke ns1.ns2
            var nsName = node.Text.Replace("::", ".");

            // Untuk nested namespace definitions (namespace app { namespace utils { ... } }),
            // walk up AST untuk compose full qualified name
            var parentNsNode = node.Parent?.Parent; // namespace_definition → parent
            while (parentNsNode is not null)
            {
                if (parentNsNode.Type == "namespace_definition")
                {
                    var parentName = parentNsNode.GetChildForField("name");
                    if (parentName is not null)
                    {
                        var parentNsName = parentName.Text.Replace("::", ".");
                        nsName = $"{parentNsName}.{nsName}";
                    }
                }
                parentNsNode = parentNsNode.Parent;
            }

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

        foreach (var match in RunQuery(CppQueries.Class, root, lang))
        {
            var node = GetCaptureNode(match, "class_name")
                    ?? GetCaptureNode(match, "struct_name");
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

        foreach (var match in RunQuery(CppQueries.Function, root, lang))
        {
            var nameNode = GetCaptureNode(match, "fn_name");
            var paramsNode = GetCaptureNode(match, "params");
            if (nameNode is null) continue;

            var line = nameNode.StartPosition.Row;

            // C++ qualified names: Class::Method → ambil nama method saja
            // dan simpan class qualifier sebagai fallback ParentChain
            var fnName = nameNode.Text;
            string? qualifiedClass = null;
            if (fnName.Contains("::"))
            {
                qualifiedClass = fnName[..fnName.LastIndexOf("::")];
                fnName = fnName[(fnName.LastIndexOf("::") + 2)..];
            }

            var paramTypes = paramsNode is not null
                ? ExtractParamTypes(CppQueries.ParameterType, paramsNode, lang)
                : "";

            // Out-of-class definitions: gunakan qualified name sebagai ParentChain
            // Post-processing akan override untuk in-class definitions
            result.Add(new FunctionInfo(
                Name: fnName,
                ParentChain: qualifiedClass,
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

        foreach (var match in RunQuery(CppQueries.FunctionCall, root, lang))
        {
            var node = GetCaptureNode(match, "call_name");
            if (node is null) continue;

            string? objectName = null;
            var parent = node.Parent;

            // field_expression → obj.method atau obj->method
            if (parent?.Type == "field_expression")
            {
                var objNode = parent.GetChildForField("argument");
                if (objNode is not null)
                    objectName = objNode.Text;
            }

            // Qualified call: Namespace::Func → ambil nama func saja
            var callName = node.Text;
            if (callName.Contains("::"))
                callName = callName[(callName.LastIndexOf("::") + 2)..];

            result.Add(new CallInfo(
                Name: callName,
                ObjectName: objectName,
                Line: node.StartPosition.Row
            ));
        }

        return result;
    }

    protected override List<TypeRefInfo> QueryTypeRefs(Node root, Language lang)
    {
        var result = new List<TypeRefInfo>();

        foreach (var capture in RunQueryCaptures(CppQueries.TypeReference, root, lang))
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

        foreach (var capture in RunQueryCaptures(CppQueries.Include, root, lang))
        {
            if (capture.Name != "include_path") continue;

            // Bersihkan <> dan "" dari path
            var path = capture.Node.Text.Trim('<', '>', '"', ' ');

            result.Add(new IncludeInfo(
                Path: path,
                Line: capture.Node.StartPosition.Row
            ));
        }

        return result;
    }
}
