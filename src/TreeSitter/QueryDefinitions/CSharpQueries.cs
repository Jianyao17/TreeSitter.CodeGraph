namespace GithubAnalyzer.Analysis.TreeSitter.QueryDefinitions;

/// <summary>
/// Tree-sitter S-expression queries untuk grammar C#.
/// </summary>
public static class CSharpQueries
{
    // === Pass 1: Declaration ===

    public const string Namespace = @"
        [
            (namespace_declaration name: (_) @ns_name)
            (file_scoped_namespace_declaration name: (_) @ns_name)
        ]";

    public const string Class = @"
        [
            (class_declaration name: (identifier) @class_name)
            (interface_declaration name: (identifier) @iface_name)
            (struct_declaration name: (identifier) @struct_name)
            (record_declaration name: (identifier) @record_name)
        ]";

    public const string Function = @"
        [
            (method_declaration
                name: (identifier) @fn_name
                parameters: (parameter_list) @params)
            (constructor_declaration
                name: (identifier) @ctor_name
                parameters: (parameter_list) @params)
            (local_function_statement
                name: (identifier) @fn_name
                parameters: (parameter_list) @params)
        ]";

    // === Pass 2: Usage ===

    public const string FunctionCall = @"
        [
            (invocation_expression
                function: (identifier) @call_name)
            (invocation_expression
                function: (member_access_expression
                    name: (identifier) @call_name))
        ]";

    public const string TypeReference = @"
        [
            (object_creation_expression type: (identifier) @type_ref)
            (variable_declaration type: (identifier) @type_ref)
            (property_declaration type: (identifier) @type_ref)
            (parameter type: (identifier) @type_ref)
            (generic_name (identifier) @type_ref)
            (type_argument_list (identifier) @type_ref)
            (typeof_expression type: (identifier) @type_ref)
            (member_access_expression expression: (identifier) @type_ref)
        ]";

    // === Parameter type extraction ===

    public const string ParameterType = @"
        (parameter
            type: (_) @param_type)";
}
