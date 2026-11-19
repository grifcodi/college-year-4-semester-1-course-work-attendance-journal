using AttendanceJournal.Services.Common;

namespace AttendanceJournal.ViewModels.Localization;

public enum AppLanguage
{
    Uk,
    En
}

public static class Localizer
{
    public static AppLanguage CurrentLanguage { get; set; } = AppLanguage.Uk;

    private static readonly Dictionary<string, string> UkStrings = new()
    {
        [ErrorKeys.DatabaseConflict] = "Не вдалося зберегти зміни через конфлікт даних. Оновіть список і спробуйте ще раз.",
        [ErrorKeys.DatabaseUnavailable] = "Не вдалося відкрити базу даних. Перевірте права доступу до папки профілю та спробуйте ще раз.",
        [ErrorKeys.UnexpectedError] = "Сталася неочікувана помилка. Спробуйте ще раз; подробиці записано в журнал помилок.",
        [ErrorKeys.EntityNotFound] = "Запис не знайдено. Можливо, його вже змінено. Оновіть список і спробуйте ще раз.",

        [ErrorKeys.SubjectNameRequired] = "Введіть назву дисципліни.",
        [ErrorKeys.SubjectNameTooLong] = "Назва дисципліни занадто довга. Максимум — {0} символів, скоротіть її.",
        [ErrorKeys.SubjectAlreadyExists] = "Дисципліна з такою назвою вже існує (регістр не враховується). Введіть іншу назву або відновіть наявну з архіву.",

        [ErrorKeys.GroupNameRequired] = "Введіть назву групи.",
        [ErrorKeys.GroupNameTooLong] = "Назва групи занадто довга. Максимум — {0} символів, скоротіть її.",
        [ErrorKeys.GroupYearRequired] = "Введіть навчальний рік, наприклад 2026-2027.",
        [ErrorKeys.GroupYearTooLong] = "Навчальний рік занадто довгий. Максимум — {0} символів.",
        [ErrorKeys.GroupAlreadyExists] = "Активна група «{0}» уже існує. Введіть іншу назву або заархівуйте наявну групу.",
        [ErrorKeys.GroupUnarchiveConflict] = "Не можна відновити групу «{0}»: активна група з такою назвою вже існує. Спершу перейменуйте або заархівуйте її.",

        [ErrorKeys.StudentNameRequired] = "Введіть ПІБ студента.",
        [ErrorKeys.StudentNameTooLong] = "ПІБ занадто довге. Максимум — {0} символів, скоротіть його.",
        [ErrorKeys.StudentAlreadyExists] = "Студент «{0}» уже є в цій групі (враховуючи архівованих). Перевірте список.",

        [UiTextKeys.ConfirmTitle] = "Підтвердження",
        [UiTextKeys.ConfirmYes] = "Так",
        [UiTextKeys.ConfirmNo] = "Ні",
        [UiTextKeys.ConfirmArchiveGroup] = "Заархівувати групу «{0}»? Вона зникне зі списків вибору, але всі дані збережуться.",
        [UiTextKeys.ConfirmArchiveStudent] = "Заархівувати студента «{0}»? Він не показуватиметься в нових заняттях, але історія збережеться.",
    };

    private static readonly Dictionary<string, string> EnStrings = new()
    {
        [ErrorKeys.DatabaseConflict] = "The changes could not be saved because of a data conflict. Refresh the list and try again.",
        [ErrorKeys.DatabaseUnavailable] = "The database could not be opened. Check access rights to your profile folder and try again.",
        [ErrorKeys.UnexpectedError] = "An unexpected error occurred. Try again; the details were written to the error log.",
        [ErrorKeys.EntityNotFound] = "The record was not found. It may have been changed already. Refresh the list and try again.",

        [ErrorKeys.SubjectNameRequired] = "Enter the subject name.",
        [ErrorKeys.SubjectNameTooLong] = "The subject name is too long. The maximum is {0} characters; shorten it.",
        [ErrorKeys.SubjectAlreadyExists] = "A subject with this name already exists (case is ignored). Enter a different name or restore the existing one from the archive.",

        [ErrorKeys.GroupNameRequired] = "Enter the group name.",
        [ErrorKeys.GroupNameTooLong] = "The group name is too long. The maximum is {0} characters; shorten it.",
        [ErrorKeys.GroupYearRequired] = "Enter the academic year, for example 2026-2027.",
        [ErrorKeys.GroupYearTooLong] = "The academic year is too long. The maximum is {0} characters.",
        [ErrorKeys.GroupAlreadyExists] = "An active group “{0}” already exists. Enter a different name or archive the existing group.",
        [ErrorKeys.GroupUnarchiveConflict] = "Group “{0}” cannot be restored: an active group with this name already exists. Rename or archive it first.",

        [ErrorKeys.StudentNameRequired] = "Enter the student's full name.",
        [ErrorKeys.StudentNameTooLong] = "The full name is too long. The maximum is {0} characters; shorten it.",
        [ErrorKeys.StudentAlreadyExists] = "Student “{0}” is already in this group (archived students count too). Check the list.",

        [UiTextKeys.ConfirmTitle] = "Confirmation",
        [UiTextKeys.ConfirmYes] = "Yes",
        [UiTextKeys.ConfirmNo] = "No",
        [UiTextKeys.ConfirmArchiveGroup] = "Archive group “{0}”? It will disappear from the pickers, but all its data is kept.",
        [UiTextKeys.ConfirmArchiveStudent] = "Archive student “{0}”? They will not appear in new lessons, but their history is kept.",
    };

    public static string TranslateError(Result result, AppLanguage? language = null)
    {
        if (result.IsSuccess || string.IsNullOrWhiteSpace(result.ErrorKey))
            return string.Empty;

        return Format(result.ErrorKey, language ?? CurrentLanguage, result.ErrorArgs);
    }

    public static string Translate(string key, params object[] args) =>
        Format(key, CurrentLanguage, args);

    public static string TranslateIn(AppLanguage language, string key, params object[] args) =>
        Format(key, language, args);

    private static string Format(string key, AppLanguage language, object[] args)
    {
        var strings = language == AppLanguage.En ? EnStrings : UkStrings;

        if (!strings.TryGetValue(key, out var template))
            return key;

        try
        {
            return string.Format(template, args);
        }
        catch (FormatException)
        {
            return template;
        }
    }
}