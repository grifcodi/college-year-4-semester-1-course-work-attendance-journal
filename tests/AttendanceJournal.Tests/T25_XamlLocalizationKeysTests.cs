using AttendanceJournal.Models;
using AttendanceJournal.ViewModels.Localization;
using System.Text.RegularExpressions;

namespace AttendanceJournal.Tests;

/// <summary>
/// FR-13: every key that a screen binds to (Path=[Ui_…]) has a text in both languages.
/// Without this test a typo in XAML shows the raw key on screen and nothing fails.
/// </summary>
public class T25_XamlLocalizationKeysTests
{
    private static readonly Regex KeyPattern = new(@"Path=\[(Ui_\w+)\]", RegexOptions.Compiled);

    private static string FindProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AttendanceJournal.csproj")))
            dir = dir.Parent;

        Assert.NotNull(dir);
        return dir!.FullName;
    }

    [Fact]
    public void AllKeysUsedInXaml_ExistInBothLanguages()
    {
        var root = FindProjectRoot();
        var files = Directory.GetFiles(Path.Combine(root, "Views"), "*.axaml", SearchOption.AllDirectories);
        Assert.NotEmpty(files);

        var missing = new List<string>();
        var keyCount = 0;
        foreach (var file in files)
        {
            foreach (Match match in KeyPattern.Matches(File.ReadAllText(file)))
            {
                keyCount++;
                var key = match.Groups[1].Value;
                foreach (var language in Enum.GetValues<AppLanguage>())
                {
                    if (Localizer.TranslateIn(language, key) == key)
                        missing.Add($"{Path.GetFileName(file)}: {key} ({language})");
                }
            }
        }

        Assert.True(keyCount > 50, $"Only {keyCount} keys found; is the pattern still right?");
        Assert.True(missing.Count == 0, "Keys without translation:" + Environment.NewLine + string.Join(Environment.NewLine, missing));
    }
}
