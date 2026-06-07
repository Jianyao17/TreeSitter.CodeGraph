namespace GithubAnalyzer.Fixtures.Helpers;

public class StringHelper
{
    public static string Capitalize(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return char.ToUpper(input[0]) + input[1..];
    }

    public static string Truncate(string input, int maxLength)
    {
        if (input.Length <= maxLength)
            return input;

        return input[..maxLength] + "...";
    }
}
