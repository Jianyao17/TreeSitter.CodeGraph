namespace GithubAnalyzer.Analysis.TreeSitter.QueryDefinitions;

/// <summary>
/// Tree-sitter S-expression queries untuk grammar C++.
/// </summary>
public static class CppQueries
{
    // === Pass 1: Declaration ===

    public const string Namespace = @"
        (namespace_definition
            name: (namespace_identifier) @ns_name)";

    public const string Class = @"
        [
            (class_specifier
                name: (type_identifier) @class_name)
            (struct_specifier
                name: (type_identifier) @struct_name)
        ]";

    public const string Function = @"
        [
            (function_definition
                declarator: (function_declarator
                    declarator: (identifier) @fn_name
                    parameters: (parameter_list) @params))
            (function_definition
                declarator: (function_declarator
                    declarator: (field_identifier) @fn_name
                    parameters: (parameter_list) @params))
            (function_definition
                declarator: (function_declarator
                    declarator: (qualified_identifier) @fn_name
                    parameters: (parameter_list) @params))
        ]";

    // === Pass 2: Usage ===

    public const string FunctionCall = @"
        [
            (call_expression
                function: (identifier) @call_name)
            (call_expression
                function: (field_expression
                    field: (field_identifier) @call_name))
            (call_expression
                function: (qualified_identifier) @call_name)
        ]";

    public const string TypeReference = @"
        (new_expression
            type: (type_identifier) @type_ref)";

    public const string Include = @"
        (preproc_include
            path: (_) @include_path)";

    // === Parameter type extraction ===

    public const string ParameterType = @"
        (parameter_declaration
            type: (_) @param_type)";
}
