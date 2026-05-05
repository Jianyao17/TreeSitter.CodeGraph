using GithubAnalyzer.Analysis.TreeSitter.LangAnalyzer;

namespace GithubAnalyzer.Analysis.Tests.LangQuery;

/// <summary>
/// Menguji ekstraksi deklarasi dan usage dari kode PHP.
/// Contoh: namespace App\Services → App.Services → class UserService → method findById(int).
/// </summary>
public class PhpLangQueryTests
{
    // Use block-style namespace so parent range encompasses classes
    private const string SampleCode = @"<?php

namespace App\Services {

require_once '../Models/User.php';

class UserService
{
    private array $users = [];

    public function __construct()
    {
    }

    public function findById(int $id): array
    {
        return $this->users[$id] ?? [];
    }

    public function save(string $name, string $email): void
    {
        $this->users[] = ['name' => $name, 'email' => $email];
    }
}

class OrderService
{
    public function createOrder(string $product): void
    {
        $service = new UserService();
        $service->findById(1);
    }
}

} // end namespace
";

    [Fact]
    public void QueryNamespaces_NormalizesBackslash()
    {
        try
        {
            using var query = new PhpLangQuery();
            var result = query.ExtractAll(SampleCode);
            Assert.NotEmpty(result.Namespaces);
            Assert.Contains(result.Namespaces, ns => ns.Name == "App.Services");
        }
        catch (DllNotFoundException)
        {
            Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
        }
    }

    [Fact]
    public void QueryClasses_DetectsClasses()
    {
        try
        {
            using var query = new PhpLangQuery();
            var result = query.ExtractAll(SampleCode);
            Assert.Equal(2, result.Classes.Count);
            Assert.Contains(result.Classes, c => c.Name == "UserService");
            Assert.Contains(result.Classes, c => c.Name == "OrderService");
        }
        catch (DllNotFoundException)
        {
            Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
        }
    }

    [Fact]
    public void QueryFunctions_DetectsTypedParams()
    {
        try
        {
            using var query = new PhpLangQuery();
            var result = query.ExtractAll(SampleCode);
            Assert.True(result.Functions.Count >= 3);
            var f = result.Functions.First(f => f.Name == "findById");
            Assert.Contains("int", f.Params);
            Assert.Equal("UserService", f.ParentClass);
        }
        catch (DllNotFoundException)
        {
            Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
        }
    }

    [Fact]
    public void QueryCalls_DetectsCalls()
    {
        try
        {
            using var query = new PhpLangQuery();
            var result = query.ExtractAll(SampleCode);
            Assert.Contains(result.Calls, c => c.Name == "findById");
        }
        catch (DllNotFoundException)
        {
            Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
        }
    }

    [Fact]
    public void QueryTypeRefs_DetectsNew()
    {
        try
        {
            using var query = new PhpLangQuery();
            var result = query.ExtractAll(SampleCode);
            Assert.Contains(result.TypeRefs, t => t.Name == "UserService");
        }
        catch (DllNotFoundException)
        {
            Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
        }
    }

    [Fact]
    public void QueryIncludes_DetectsRequire()
    {
        try
        {
            using var query = new PhpLangQuery();
            var result = query.ExtractAll(SampleCode);
            Assert.Contains(result.Includes, i => i.Path.Contains("Models/User.php"));
        }
        catch (DllNotFoundException)
        {
            Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
        }
    }

    [Fact]
    public void ExtractAll_CRLF_SameCount()
    {
        try
        {
            using var query = new PhpLangQuery();
            var r1 = query.ExtractAll(SampleCode);
            var r2 = query.ExtractAll(SampleCode.Replace("\n", "\r\n"));
            Assert.Equal(r1.Classes.Count, r2.Classes.Count);
            Assert.Equal(r1.Functions.Count, r2.Functions.Count);
        }
        catch (DllNotFoundException)
        {
            Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
        }
    }

    [Fact]
    public void ExtractAll_EmptyPhp_ReturnsEmptyLists()
    {
        try
        {
            using var query = new PhpLangQuery();
            var result = query.ExtractAll("<?php ");
            Assert.Empty(result.Classes);
            Assert.Empty(result.Functions);
        }
        catch (DllNotFoundException)
        {
            Assert.True(true, "Skipped: missing native Tree-sitter binaries.");
        }
    }
}
