using GithubAnalyzer.Analysis.Domain.TreeSitter;
using GithubAnalyzer.Analysis.Languages;

namespace GithubAnalyzer.Analysis.Tests.Languages;

public class LanguageRegistryTests
{
    private readonly LanguageRegistry _sut = LanguageRegistry.Default;

    [Fact]
    public void Default_ContainsAllSupportedLanguages()
    {
        var languages = _sut.GetAllLanguages();

        Assert.Contains(languages, l => l.Language == AnalysisLanguage.CSharp);
        Assert.Contains(languages, l => l.Language == AnalysisLanguage.JavaScript);
        Assert.Contains(languages, l => l.Language == AnalysisLanguage.Php);
        Assert.Contains(languages, l => l.Language == AnalysisLanguage.Cpp);
    }

    [Theory]
    [InlineData(AnalysisLanguage.CSharp, "C#", "c-sharp")]
    [InlineData(AnalysisLanguage.JavaScript, "JavaScript", "javascript")]
    [InlineData(AnalysisLanguage.Php, "PHP", "php")]
    [InlineData(AnalysisLanguage.Cpp, "C++", "cpp")]
    public void GetDefinition_ReturnsCorrectMetadata(
        AnalysisLanguage language, string expectedName, string expectedTreeSitterId)
    {
        var def = _sut.GetDefinition(language);

        Assert.Equal(expectedName, def.DisplayName);
        Assert.Equal(expectedTreeSitterId, def.TreeSitterLanguageId);
        Assert.NotEmpty(def.FileExtensions);
    }

    [Theory]
    [InlineData(".cs", AnalysisLanguage.CSharp)]
    [InlineData("cs", AnalysisLanguage.CSharp)]
    [InlineData(".CS", AnalysisLanguage.CSharp)]
    [InlineData(".js", AnalysisLanguage.JavaScript)]
    [InlineData(".ts", AnalysisLanguage.JavaScript)]
    [InlineData("TS", AnalysisLanguage.JavaScript)]
    [InlineData(".php", AnalysisLanguage.Php)]
    [InlineData(".cpp", AnalysisLanguage.Cpp)]
    [InlineData(".cxx", AnalysisLanguage.Cpp)]
    [InlineData(".cc", AnalysisLanguage.Cpp)]
    [InlineData(".h", AnalysisLanguage.Cpp)]
    [InlineData(".hpp", AnalysisLanguage.Cpp)]
    public void FindByExtension_ReturnsCorrectLanguage(string extension, AnalysisLanguage expected)
    {
        var def = _sut.FindByExtension(extension);

        Assert.NotNull(def);
        Assert.Equal(expected, def.Language);
    }

    [Theory]
    [InlineData(".py")]
    [InlineData(".rs")]
    [InlineData(".go")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void FindByExtension_UnknownOrEmpty_ReturnsNull(string? extension)
    {
        var def = _sut.FindByExtension(extension!);

        Assert.Null(def);
    }

    [Fact]
    public void ExtensionMethods_WorkConsistentlyWithRegistry()
    {
        Assert.Equal("c-sharp", AnalysisLanguage.CSharp.GetTreeSitterId());
        Assert.Equal("C#", AnalysisLanguage.CSharp.GetDisplayName());
        Assert.Contains(".cs", AnalysisLanguage.CSharp.GetSupportedExtensions());
    }
}
