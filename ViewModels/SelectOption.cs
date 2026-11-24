namespace AttendanceJournal.ViewModels;

/// <summary>An item of a combo box: a value and the text shown for it.</summary>
public sealed record SelectOption<T>(T Value, string Text)
{
    public override string ToString() => Text;
}
