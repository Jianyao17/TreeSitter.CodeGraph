namespace GithubAnalyzer.Analysis.TreeSitter.QueryDefinitions;

/// <summary>
/// Tree-sitter S-expression queries untuk grammar PHP.
/// </summary>
public static class PhpQueries
{
    // === Pass 1: Declaration ===

    public const string Namespace = @"
        (namespace_definition
            name: (namespace_name) @ns_name)";

    public const string Class = @"
        [
            (class_declaration
                name: (name) @class_name)
            (interface_declaration
                name: (name) @iface_name)
            (trait_declaration
                name: (name) @trait_name)
        ]";

    public const string Function = @"
        [
            (method_declaration
                name: (name) @fn_name
                parameters: (formal_parameters) @params)
            (function_definition
                name: (name) @fn_name
                parameters: (formal_parameters) @params)
        ]";

    // === Pass 2: Usage ===

    public const string FunctionCall = @"
        [
            (function_call_expression
                function: (name) @call_name)
            (function_call_expression
                function: (qualified_name) @call_name)
            (member_call_expression
                name: (name) @call_name)
            (scoped_call_expression
                name: (name) @call_name)
        ]";

    public const string TypeReference = @"
        (object_creation_expression
            (name) @type_ref)";

    public const string Include = @"
        [
            (include_expression (_) @include_path)
            (include_once_expression (_) @include_path)
            (require_expression (_) @include_path)
            (require_once_expression (_) @include_path)
        ]";

    // === Parameter type extraction ===

    public const string ParameterType = @"
        (simple_parameter
            type: (_) @param_type)";
}
