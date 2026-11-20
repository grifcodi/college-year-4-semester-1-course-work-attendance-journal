using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;
using AttendanceJournal.ViewModels.Localization;
using System.Reflection;

namespace AttendanceJournal.Tests;

public class T13_LocalizationTests
{
    private static List<string> AllKeys() =>
        new[] { typeof(ErrorKeys), typeof(UiTextKeys) }
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

    [Fact]
    public void KeyDiscovery_FindsKeys_AndKeyValuesAreUnique()
    {
        var keys = AllKeys();

        Assert.NotEmpty(keys);
        var duplicates = keys.GroupBy(k => k).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Assert.True(duplicates.Count == 0, "Однакові значення ключів: " + string.Join(", ", duplicates));
    }

    [Fact]
    public void EveryKey_HasTranslation_InEveryLanguage()
    {
        var missing = new List<string>();

        foreach (var language in Enum.GetValues<AppLanguage>())
        {
            foreach (var key in AllKeys())
            {
                var text = Localizer.TranslateError(Result.Failure(key, 100), language);
                if (string.IsNullOrWhiteSpace(text) || text == key)
                    missing.Add($"{language}: {key}");
            }
        }

        Assert.True(missing.Count == 0,
            "Немає перекладу для:" + Environment.NewLine + string.Join(Environment.NewLine, missing));
    }

    [Fact]
    public void Messages_AreDistinct_InEveryLanguage()
    {
        var duplicates = new List<string>();

        foreach (var language in Enum.GetValues<AppLanguage>())
        {
            var sameText = AllKeys()
                .GroupBy(k => Localizer.TranslateError(Result.Failure(k, "x"), language))
                .Where(g => g.Count() > 1);

            duplicates.AddRange(sameText.Select(g => $"{language}: {string.Join(", ", g)}"));
        }

        Assert.True(duplicates.Count == 0,
            "Однаковий текст для різних ключів:" + Environment.NewLine + string.Join(Environment.NewLine, duplicates));
    }

    [Fact]
    public void LengthArgument_IsInsertedIntoMessage()
    {
        var result = Result.Failure(ErrorKeys.SubjectNameTooLong, 150);

        Assert.Contains("150", Localizer.TranslateError(result, AppLanguage.Uk));
        Assert.Contains("150", Localizer.TranslateError(result, AppLanguage.En));
    }

    [Fact]
    public void NameArgument_IsInsertedIntoMessage()
    {
        var result = Result.Failure(ErrorKeys.GroupAlreadyExists, "ІПЗ-21");

        Assert.Contains("ІПЗ-21", Localizer.TranslateError(result, AppLanguage.Uk));
        Assert.Contains("ІПЗ-21", Localizer.TranslateError(result, AppLanguage.En));
    }

    [Fact]
    public void MissingArgument_DoesNotThrow()
    {
        var result = Result.Failure(ErrorKeys.GroupAlreadyExists);

        var text = Localizer.TranslateError(result, AppLanguage.Uk);

        Assert.False(string.IsNullOrWhiteSpace(text));
    }

    [Fact]
    public void Success_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, Localizer.TranslateError(Result.Success(), AppLanguage.Uk));
    }

    [Fact]
    public void UnknownKey_ReturnsTheKeyItself()
    {
        var result = Result.Failure("Err_Unknown_Key");

        Assert.Equal("Err_Unknown_Key", Localizer.TranslateError(result, AppLanguage.En));
    }
}