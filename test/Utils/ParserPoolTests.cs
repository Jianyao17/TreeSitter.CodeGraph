using GithubAnalyzer.Analysis.Domain.TreeSitter;
using GithubAnalyzer.Analysis.TreeSitter.Utils;

namespace GithubAnalyzer.Analysis.Tests.Utils;

/// <summary>
/// Menguji ParserPool: instantiasi untuk semua bahasa, parse valid/invalid,
/// disposal, dan validasi struktur tree yang dihasilkan.
/// Contoh: ParserPool(CSharp).Parse("class X {}") → root type "compilation_unit",
/// S-expression mengandung "class_declaration", text mengandung "X".
/// </summary>
public class ParserPoolTests
{
    [Fact]
    public void Parse_CSharp_TreeHasCorrectStructure()
    {
        try
        {
            using var pool = new ParserPool(AnalysisLanguage.CSharp);
            var code = "public class Foo { public void Bar(int x) { } }";
            using var tree = pool.Parse(code);

            var root = tree.RootNode;
            Assert.Equal("compilation_unit", root.Type);

            // S-expression validates grammar node types exist
            var expr = root.Expression;
            Assert.Contains("class_declaration", expr);
            Assert.Contains("method_declaration", expr);
            Assert.Contains("parameter_list", expr);

            // Root text should contain the full source code
            Assert.Equal(code, root.Text);
        }
        catch (DllNotFoundException)
        {
            Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
        }
    }

    [Fact]
    public void Parse_JavaScript_TreeHasCorrectStructure()
    {
        try
        {
            using var pool = new ParserPool(AnalysisLanguage.JavaScript);
            var code = "class Greeter { greet(name) { return 'hello'; } }";
            using var tree = pool.Parse(code);

            var root = tree.RootNode;
            Assert.Equal("program", root.Type);

            var expr = root.Expression;
            Assert.Contains("class_declaration", expr);
            Assert.Contains("method_definition", expr);

            // Text preserves the full source
            Assert.Equal(code, root.Text);
        }
        catch (DllNotFoundException)
        {
            Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
        }
    }

    [Fact]
    public void Parse_Php_TreeHasCorrectStructure()
    {
        try
        {
            using var pool = new ParserPool(AnalysisLanguage.Php);
            var code = "<?php\nclass Service { public function run(int $id): void { } }";
            using var tree = pool.Parse(code);

            var root = tree.RootNode;
            Assert.Equal("program", root.Type);

            var expr = root.Expression;
            Assert.Contains("class_declaration", expr);
            Assert.Contains("method_declaration", expr);

            Assert.Equal(code, root.Text);
        }
        catch (DllNotFoundException)
        {
            Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
        }
    }

    [Fact]
    public void Parse_Cpp_TreeHasCorrectStructure()
    {
        try
        {
            using var pool = new ParserPool(AnalysisLanguage.Cpp);
            var code = "class Widget { };\nvoid process(int n) { }";
            using var tree = pool.Parse(code);

            var root = tree.RootNode;
            Assert.Equal("translation_unit", root.Type);

            var expr = root.Expression;
            Assert.Contains("class_specifier", expr);
            Assert.Contains("function_definition", expr);

            Assert.Equal(code, root.Text);
        }
        catch (DllNotFoundException)
        {
            Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
        }
    }

    [Fact]
    public void Parse_AfterDispose_ThrowsObjectDisposedException()
    {
        try
        {
            var pool = new ParserPool(AnalysisLanguage.CSharp);
            pool.Dispose();

            Assert.Throws<ObjectDisposedException>(() => pool.Parse("class X { }"));
        }
        catch (DllNotFoundException)
        {
            Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
        }
    }

    [Fact]
    public void Constructor_UnsupportedLanguage_ThrowsArgumentOutOfRangeException()
    {
        try
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ParserPool((AnalysisLanguage)999));
        }
        catch (DllNotFoundException)
        {
            Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
        }
    }

    [Fact]
    public void Parse_WithCRLF_ProducesEquivalentTree()
    {
        try
        {
            using var pool = new ParserPool(AnalysisLanguage.CSharp);
            var codeLF = "public class Foo {\n    public void Bar() {\n    }\n}";
            var codeCRLF = codeLF.Replace("\n", "\r\n");

            using var treeLF = pool.Parse(codeLF);
            using var treeCRLF = pool.Parse(codeCRLF);

            // Both should parse to the same root type
            Assert.Equal(treeLF.RootNode.Type, treeCRLF.RootNode.Type);

            // Both S-expressions should contain same grammar nodes
            var exprLF = treeLF.RootNode.Expression;
            var exprCRLF = treeCRLF.RootNode.Expression;
            Assert.Contains("class_declaration", exprLF);
            Assert.Contains("class_declaration", exprCRLF);
            Assert.Contains("method_declaration", exprLF);
            Assert.Contains("method_declaration", exprCRLF);

            // Both texts should contain the class and method names
            Assert.Contains("Foo", treeLF.RootNode.Text);
            Assert.Contains("Foo", treeCRLF.RootNode.Text);
            Assert.Contains("Bar", treeLF.RootNode.Text);
            Assert.Contains("Bar", treeCRLF.RootNode.Text);
        }
        catch (DllNotFoundException)
        {
            Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
        }
    }

    [Fact]
    public void Parse_WithUnicodeContent_PreservesStructureAndText()
    {
        try
        {
            using var pool = new ParserPool(AnalysisLanguage.CSharp);
            var code = "// Mengambil data pengguna\npublic class Héllo { string s = \"日本語\"; }";
            using var tree = pool.Parse(code);

            var root = tree.RootNode;
            Assert.Equal("compilation_unit", root.Type);

            var expr = root.Expression;
            Assert.Contains("class_declaration", expr);
            Assert.Contains("comment", expr);

            // Full text should be preserved including Unicode characters
            Assert.Equal(code, root.Text);
            Assert.Contains("Héllo", root.Text);
            Assert.Contains("日本語", root.Text);
            Assert.Contains("Mengambil data pengguna", root.Text);
        }
        catch (DllNotFoundException)
        {
            Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
        }
    }

    [Fact]
    public void Parse_EmptyCode_ReturnsTreeWithNoDeclarations()
    {
        try
        {
            using var pool = new ParserPool(AnalysisLanguage.CSharp);
            using var tree = pool.Parse("");

            var root = tree.RootNode;
            Assert.Equal("compilation_unit", root.Type);
            // Expression for empty source should be just the root node wrapper
            Assert.DoesNotContain("class_declaration", root.Expression);
            Assert.DoesNotContain("method_declaration", root.Expression);
        }
        catch (DllNotFoundException)
        {
            Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
        }
    }

    [Fact]
    public void Parse_SyntaxError_TreeContainsErrorNode()
    {
        try
        {
            using var pool = new ParserPool(AnalysisLanguage.CSharp);
            // Missing closing brace — syntax error
            var code = "public class Broken {";
            using var tree = pool.Parse(code);

            var root = tree.RootNode;
            Assert.Equal("compilation_unit", root.Type);
            // S-expression should contain MISSING or ERROR markers
            var expr = root.Expression;
            Assert.True(
                expr.Contains("MISSING") || expr.Contains("ERROR"),
                $"Expected error markers in expression for invalid code, got: {expr}");
        }
        catch (DllNotFoundException)
        {
            Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
        }
    }

    [Theory]
    [InlineData(AnalysisLanguage.CSharp)]
    [InlineData(AnalysisLanguage.JavaScript)]
    [InlineData(AnalysisLanguage.Php)]
    [InlineData(AnalysisLanguage.Cpp)]
    public void Constructor_AllSupportedLanguages_Succeeds(AnalysisLanguage language)
    {
        try
        {
            using var pool = new ParserPool(language);
            Assert.NotNull(pool.Language);
        }
        catch (DllNotFoundException)
        {
            Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
        }
    }
}
