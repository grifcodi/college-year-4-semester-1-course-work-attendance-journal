using AttendanceJournal.Services.Common;
using AttendanceJournal.ViewModels.Localization;
using System.Reflection;

namespace AttendanceJournal.Tests;

public class T13_LocalizationTests
{
    public static IEnumerable<object[]> AllKeysAndLanguages()
    {
        var keys = new[] { typeof(ErrorKeys), typeof(UiTextKeys) }
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!);

        foreach (var key in keys)
            foreach (var language in Enum.GetValues<AppLanguage>())
                yield return [key, language];
    }

    public static TheoryData<string, AppLanguage> KeyTheoryData()
    {
        var data = new TheoryData<string, AppLanguage>();
        foreach (var row in AllKeysAndLanguages())
            data.Add((string)row[0], (AppLanguage)row[1]);
        return data;
    }

    [Theory]
    [MemberData(nameof(KeyTheoryData))]
    public void EveryErrorKey_HasTranslation_InEveryLanguage(string key, AppLanguage language)
    {
        var text = Localizer.TranslateError(Result.Failure(key, 100), language);

        Assert.False(string.IsNullOrWhiteSpace(text));
        Assert.NotEqual(key, text);
    }

    [Theory]
    [InlineData(AppLanguage.Uk)]
    [InlineData(AppLanguage.En)]
    public void Messages_AreDistinctPerKey(AppLanguage language)
    {
        var keys = AllKeysAndLanguages()
            .Where(r => (AppLanguage)r[1] == language)
            .Select(r => (string)r[0]);

        var texts = keys.Select(k => Localizer.TranslateError(Result.Failure(k, "x"), language)).ToList();

        Assert.Equal(texts.Count, texts.Distinct().Count());
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