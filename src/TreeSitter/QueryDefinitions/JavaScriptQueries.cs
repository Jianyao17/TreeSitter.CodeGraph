namespace GithubAnalyzer.Analysis.TreeSitter.QueryDefinitions;

/// <summary>
/// Tree-sitter S-expression queries untuk grammar JavaScript.
/// </summary>
public static class JavaScriptQueries
{
    // === Pass 1: Declaration ===
    // JavaScript tidak punya namespace, gunakan folder hierarchy.

    public const string Class = @"
        (class_declaration
            name: (identifier) @class_name)";

    public const string Function = @"
        [
            (function_declaration
                name: (identifier) @fn_name
                parameters: (formal_parameters) @params)
            (method_definition
                name: (property_identifier) @fn_name
                parameters: (formal_parameters) @params)
        ]";

    /// <summary>
    /// Arrow function dan function expression yang di-assign ke variable.
    /// Capture variable name sebagai function name.
    /// </summary>
    public const string ArrowFunction = @"
        [
            (variable_declarator
                name: (identifier) @fn_name
                value: (arrow_function
                    parameters: (formal_parameters) @params))
            (variable_declarator
                name: (identifier) @fn_name
                value: (function_expression
                    parameters: (formal_parameters) @params))
        ]";

    // === Pass 2: Usage ===

    public const string FunctionCall = @"
        [
            (call_expression
                function: (identifier) @call_name)
            (call_expression
                function: (member_expression
                    property: (property_identifier) @call_name))
        ]";

    public const string TypeReference = @"
        (new_expression
            constructor: (identifier) @type_ref)";

    public const string Import = @"
        (import_statement
            source: (string (string_fragment) @import_path))";
}
