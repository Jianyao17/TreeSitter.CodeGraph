using TreeSitter;
using GithubAnalyzer.Analysis.Domain.TreeSitter;
using GithubAnalyzer.Analysis.TreeSitter.QueryDefinitions;

namespace GithubAnalyzer.Analysis.TreeSitter.LangAnalyzer;

/// <summary>
/// Implementasi query tree-sitter untuk bahasa JavaScript.
/// Menggunakan folder hierarchy (bukan namespace).
/// Mendukung import statements.
/// Parameter selalu tanpa tipe — format: functionName()
/// </summary>
public sealed class JavaScriptLangQuery : BaseLangQuery
{
    public JavaScriptLangQuery() : base(AnalysisLanguage.JavaScript) { }

    public override bool UsesNamespace => false;

    protected override List<NamespaceInfo> QueryNamespaces(Node root, Language lang)
    {
        // JavaScript tidak punya namespace
        return [];
    }

    protected override List<ClassInfo> QueryClasses(Node root, Language lang)
    {
        var result = new List<ClassInfo>();

        foreach (var capture in RunQueryCaptures(JavaScriptQueries.Class, root, lang))
        {
            if (capture.Name != "class_name") continue;

            result.Add(new ClassInfo(
                Name: capture.Node.Text,
                ParentNamespace: null,
                StartLine: capture.Node.StartPosition.Row,
                EndLine: capture.Node.Parent?.EndPosition.Row ?? capture.Node.EndPosition.Row
            ));
        }

        return result;
    }

    protected override List<FunctionInfo> QueryFunctions(Node root, Language lang)
    {
        var result = new List<FunctionInfo>();
        var classes = QueryClasses(root, lang);

        // Regular functions dan methods
        foreach (var match in RunQuery(JavaScriptQueries.Function, root, lang))
        {
            var nameNode = GetCaptureNode(match, "fn_name");
            if (nameNode is null) continue;

            var line = nameNode.StartPosition.Row;

            result.Add(new FunctionInfo(
                Name: nameNode.Text,
                ParentClass: FindParentClass(line, classes),
                ParentNamespace: null,
                Params: "", // JS tanpa type annotation
                StartLine: line,
                EndLine: nameNode.Parent?.EndPosition.Row ?? nameNode.EndPosition.Row
            ));
        }

        // Arrow functions dan function expressions
        foreach (var match in RunQuery(JavaScriptQueries.ArrowFunction, root, lang))
        {
            var nameNode = GetCaptureNode(match, "fn_name");
            if (nameNode is null) continue;

            var line = nameNode.StartPosition.Row;

            result.Add(new FunctionInfo(
                Name: nameNode.Text,
                ParentClass: FindParentClass(line, classes),
                ParentNamespace: null,
                Params: "",
                StartLine: line,
                EndLine: nameNode.Parent?.EndPosition.Row ?? nameNode.EndPosition.Row
            ));
        }

        return result;
    }

    protected override List<CallInfo> QueryCalls(Node root, Language lang)
    {
        var result = new List<CallInfo>();

        foreach (var match in RunQuery(JavaScriptQueries.FunctionCall, root, lang))
        {
            var node = GetCaptureNode(match, "call_name");
            if (node is null) continue;

            // Deteksi object name dari member_expression
            string? objectName = null;
            var parent = node.Parent;
            if (parent?.Type == "member_expression")
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

        foreach (var capture in RunQueryCaptures(JavaScriptQueries.TypeReference, root, lang))
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

        foreach (var capture in RunQueryCaptures(JavaScriptQueries.Import, root, lang))
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
