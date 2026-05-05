using GithubAnalyzer.Analysis.TreeSitter.Utils;

namespace GithubAnalyzer.Analysis.Tests.Utils;

/// <summary>
/// Menguji PathId static methods: Build, ForFolder, ForNamespace, ForFile,
/// FormatFunction, Normalize, dan konsistensi cross-platform.
/// Contoh: PathId.Build("src/User.cs", "MyApp", "UserService") → "src/User.cs::MyApp.UserService".
/// </summary>
public class PathIdTests
{
    [Fact]
    public void Build_WithSymbolPath_ReturnsCorrectFormat()
    {
        var result = PathId.Build("src/Controllers/UserController.cs", "MyApp.Controllers", "UserController");
        Assert.Equal("src/Controllers/UserController.cs::MyApp.Controllers.UserController", result);
    }

    [Fact]
    public void Build_WithoutSymbolPath_OmitsDot()
    {
        var result = PathId.Build("src/User.cs", null, "User");
        Assert.Equal("src/User.cs::User", result);
    }

    [Fact]
    public void Build_WithEmptySymbolPath_OmitsDot()
    {
        var result = PathId.Build("src/User.cs", "", "User");
        Assert.Equal("src/User.cs::User", result);
    }

    [Fact]
    public void ForFolder_EndsWithDoubleColon()
    {
        var result = PathId.ForFolder("src/Controllers");
        Assert.Equal("src/Controllers::", result);
        Assert.EndsWith("::", result);
    }

    [Fact]
    public void ForFolder_NormalizesBackslash()
    {
        var result = PathId.ForFolder("src\\Controllers");
        Assert.Equal("src/Controllers::", result);
        Assert.DoesNotContain("\\", result);
    }

    [Fact]
    public void ForNamespace_StartsWithDoubleColon()
    {
        var result = PathId.ForNamespace("App.Services");
        Assert.Equal("::App.Services", result);
        Assert.StartsWith("::", result);
    }

    [Fact]
    public void ForFile_NormalizesOnly_NoDoubleColon()
    {
        var result = PathId.ForFile("src\\Controllers\\UserController.cs");
        Assert.Equal("src/Controllers/UserController.cs", result);
        Assert.DoesNotContain("::", result);
    }

    [Theory]
    [InlineData("GetUser", "", "GetUser()")]
    [InlineData("GetUser", "int", "GetUser(int)")]
    [InlineData("Save", "int,string", "Save(int,string)")]
    public void FormatFunction_FormatsCorrectly(string name, string paramTypes, string expected)
    {
        var result = PathId.FormatFunction(name, paramTypes);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Normalize_BackslashToForwardSlash()
    {
        Assert.Equal("src/User.cs", PathId.Normalize("src\\User.cs"));
    }

    [Fact]
    public void Normalize_MixedSeparators()
    {
        Assert.Equal("src/sub/User.cs", PathId.Normalize("src\\sub/User.cs"));
    }

    [Fact]
    public void Normalize_AlreadyNormalized_Unchanged()
    {
        Assert.Equal("src/User.cs", PathId.Normalize("src/User.cs"));
    }

    [Fact]
    public void Normalize_PathWithSpaces()
    {
        Assert.Equal("my project/src/User.cs", PathId.Normalize("my project\\src\\User.cs"));
    }

    [Fact]
    public void Normalize_CrossPlatformSeparator()
    {
        // Construct path with OS separator
        var osPath = $"src{Path.DirectorySeparatorChar}Controllers{Path.DirectorySeparatorChar}User.cs";
        var normalized = PathId.Normalize(osPath);
        Assert.DoesNotContain("\\", normalized);
        Assert.Contains("/", normalized);
    }

    [Fact]
    public void Build_ConsistencyWithAnalyzer_ManualBuildMatchesExpected()
    {
        // Manual PathId construction matching what TreeSitterAnalyzer would produce
        // for a C# file at Fixtures/CSharp/Services/UserService.cs with namespace GithubAnalyzer.Fixtures.Services, class UserService
        var relativePath = "CSharp/Services/UserService.cs";
        var ns = "GithubAnalyzer.Fixtures.Services";
        var className = "UserService";

        var classPathId = PathId.Build(relativePath, ns, className);
        Assert.Equal("CSharp/Services/UserService.cs::GithubAnalyzer.Fixtures.Services.UserService", classPathId);

        // Function PathId: method FindById(int) inside UserService
        var funcLabel = PathId.FormatFunction("FindById", "int");
        var symbolPath = $"{ns}.{className}";
        var funcPathId = PathId.Build(relativePath, symbolPath, funcLabel);
        Assert.Equal("CSharp/Services/UserService.cs::GithubAnalyzer.Fixtures.Services.UserService.FindById(int)", funcPathId);
    }
}
