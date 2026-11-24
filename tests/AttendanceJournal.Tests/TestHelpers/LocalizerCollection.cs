namespace AttendanceJournal.Tests.TestHelpers;

/// <summary>
/// Localizer.CurrentLanguage is global. Tests that read or change it run one after another
/// so they cannot see each other's language.
/// </summary>
[CollectionDefinition(Name)]
public sealed class LocalizerCollection
{
    public const string Name = "Localizer";
}
