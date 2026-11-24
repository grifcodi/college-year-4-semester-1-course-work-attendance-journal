using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;

namespace AttendanceJournal.ViewModels.Localization;

public static class Localizer
{
    private static AppLanguage _currentLanguage = AppLanguage.Uk;

    public static AppLanguage CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage == value)
                return;

            _currentLanguage = value;
            LocalizationSource.Instance.NotifyLanguageChanged();
        }
    }

    private static readonly Dictionary<string, string> UkStrings = new()
    {
        [ErrorKeys.DatabaseConflict] = "Не вдалося зберегти зміни через конфлікт даних. Оновіть список і спробуйте ще раз.",
        [ErrorKeys.DatabaseUnavailable] = "Не вдалося відкрити базу даних. Перевірте права доступу до папки профілю та спробуйте ще раз.",
        [ErrorKeys.UnexpectedError] = "Сталася неочікувана помилка. Спробуйте ще раз; подробиці записано в журнал помилок.",
        [ErrorKeys.EntityNotFound] = "Запис не знайдено. Можливо, його вже змінено. Оновіть список і спробуйте ще раз.",
        [ErrorKeys.DatabaseBusy] = "База даних тимчасово заблокована іншою операцією. Зачекайте мить і спробуйте ще раз.",
        [ErrorKeys.StudentGroupArchived] = "Неможливо додати студента до архівної групи. Спершу відновіть групу з архіву.",

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

        [UiTextKeys.AppTitle] = "Електронний журнал обліку відвідуваності",
        [UiTextKeys.NavHeader] = "Журнал",
        [UiTextKeys.NavGroups] = "Групи та студенти",
        [UiTextKeys.RetryButton] = "Повторити",

        [UiTextKeys.GroupsHeader] = "Групи",
        [UiTextKeys.ArchiveFilter] = "Архів",
        [UiTextKeys.GroupNamePlaceholder] = "Назва групи (напр. ІПЗ-21)",
        [UiTextKeys.AcademicYearPlaceholder] = "Навчальний рік (напр. 2026-2027)",
        [UiTextKeys.SaveButton] = "Зберегти",
        [UiTextKeys.CancelButton] = "Скасувати",
        [UiTextKeys.EditButton] = "Редагувати",
        [UiTextKeys.ArchiveRestoreButton] = "Архів / Відновити",
        [UiTextKeys.ArchivedBadge] = "АРХІВ",

        [UiTextKeys.StudentsHeader] = "Студенти обраної групи",
        [UiTextKeys.ShowArchivedStudentsFilter] = "Показати відрахованих / архів",
        [UiTextKeys.StudentFullNamePlaceholder] = "ПІБ студента (Прізвище Ім'я По батькові)",
        [UiTextKeys.SaveStudentButton] = "Зберегти студента",

        [ErrorKeys.LessonInvalidPairNumber] = "Номер пари має бути від 1 до 8.",
        [ErrorKeys.LessonTopicTooLong] = "Тема заняття занадто довга. Максимум — {0} символів, скоротіть її.",
        [ErrorKeys.LessonDuplicateSlot] = "Для цієї групи вже існує заняття на {0} (пара №{1}). Оберіть інший номер пари або дату.",

        [ErrorKeys.AttendanceInvalidGrade] = "Оцінка має бути цілим числом від 0 до 100.",
        [ErrorKeys.AttendanceNoteTooLong] = "Примітка занадто довга. Максимум — {0} символів.",
        [ErrorKeys.AttendanceReasonRequired] = "Вкажіть причину редагування відмітки.",
        [ErrorKeys.AttendanceReasonTooShort] = "Причина редагування занадто коротка. Мінімум — {0} символів.",
        [ErrorKeys.AttendanceReasonTooLong] = "Причина редагування занадто довга. Максимум — {0} символів.",
        [ErrorKeys.AttendanceStudentArchived] = "Студент перебуває в архіві та не може бути відмічений на новому занятті.",
        [ErrorKeys.AttendanceStudentWrongGroup] = "Студент не належить до групи цього заняття.",
        [ErrorKeys.AttendanceVersionNotFound] = "Вказану версію запису відвідуваності не знайдено.",
        [ErrorKeys.AttendanceNoChange] = "Нічого не змінено. Змініть статус, оцінку або примітку.",

        [UiTextKeys.NavSettings] = "Налаштування",
        [UiTextKeys.SettingsHeader] = "Налаштування інтерфейсу",
        [UiTextKeys.SettingsLanguageLabel] = "Мова інтерфейсу",
        [UiTextKeys.SettingsLanguageDescription] = "Змінює мову меню, повідомлень про помилки та підписів без перезапуску.",
        [UiTextKeys.SettingsLanguageUk] = "Українська",
        [UiTextKeys.SettingsLanguageEn] = "English",
        [UiTextKeys.SettingsThemeLabel] = "Тема оформлення",
        [UiTextKeys.SettingsThemeDescription] = "Перемикає світлу та темну кольорову схему вікна.",
        [UiTextKeys.SettingsThemeLight] = "Світла",
        [UiTextKeys.SettingsThemeDark] = "Темна",
    };

    private static readonly Dictionary<string, string> EnStrings = new()
    {
        [ErrorKeys.DatabaseConflict] = "The changes could not be saved because of a data conflict. Refresh the list and try again.",
        [ErrorKeys.DatabaseUnavailable] = "The database could not be opened. Check access rights to your profile folder and try again.",
        [ErrorKeys.UnexpectedError] = "An unexpected error occurred. Try again; the details were written to the error log.",
        [ErrorKeys.EntityNotFound] = "The record was not found. It may have been changed already. Refresh the list and try again.",
        [ErrorKeys.DatabaseBusy] = "The database is temporarily busy or locked. Wait a moment and try again.",
        [ErrorKeys.StudentGroupArchived] = "Cannot add a student to an archived group. Restore the group from the archive first.",

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

        [UiTextKeys.AppTitle] = "Electronic Attendance Journal",
        [UiTextKeys.NavHeader] = "Journal",
        [UiTextKeys.NavGroups] = "Groups & Students",
        [UiTextKeys.RetryButton] = "Retry",

        [UiTextKeys.GroupsHeader] = "Groups",
        [UiTextKeys.ArchiveFilter] = "Archive",
        [UiTextKeys.GroupNamePlaceholder] = "Group name (e.g. SE-21)",
        [UiTextKeys.AcademicYearPlaceholder] = "Academic year (e.g. 2026-2027)",
        [UiTextKeys.SaveButton] = "Save",
        [UiTextKeys.CancelButton] = "Cancel",
        [UiTextKeys.EditButton] = "Edit",
        [UiTextKeys.ArchiveRestoreButton] = "Archive / Restore",
        [UiTextKeys.ArchivedBadge] = "ARCHIVED",

        [UiTextKeys.StudentsHeader] = "Students in Selected Group",
        [UiTextKeys.ShowArchivedStudentsFilter] = "Show archived",
        [UiTextKeys.StudentFullNamePlaceholder] = "Student's full name",
        [UiTextKeys.SaveStudentButton] = "Save student",

        [ErrorKeys.LessonInvalidPairNumber] = "Pair number must be between 1 and 8.",
        [ErrorKeys.LessonTopicTooLong] = "The lesson topic is too long. The maximum is {0} characters; shorten it.",
        [ErrorKeys.LessonDuplicateSlot] = "A lesson for this group already exists on {0} (pair #{1}). Choose another pair number or date.",

        [ErrorKeys.AttendanceInvalidGrade] = "Grade must be an integer between 0 and 100.",
        [ErrorKeys.AttendanceNoteTooLong] = "The note is too long. The maximum is {0} characters.",
        [ErrorKeys.AttendanceReasonRequired] = "Enter a reason for editing the attendance mark.",
        [ErrorKeys.AttendanceReasonTooShort] = "The edit reason is too short. The minimum is {0} characters.",
        [ErrorKeys.AttendanceReasonTooLong] = "The edit reason is too long. The maximum is {0} characters.",
        [ErrorKeys.AttendanceStudentArchived] = "The student is archived and cannot be marked in new lessons.",
        [ErrorKeys.AttendanceStudentWrongGroup] = "The student does not belong to this lesson's group.",
        [ErrorKeys.AttendanceVersionNotFound] = "The specified attendance record version was not found.",
        [ErrorKeys.AttendanceNoChange] = "Nothing was changed. Change the status, grade or note.",

        [UiTextKeys.NavSettings] = "Settings",
        [UiTextKeys.SettingsHeader] = "Interface Settings",
        [UiTextKeys.SettingsLanguageLabel] = "Interface Language",
        [UiTextKeys.SettingsLanguageDescription] = "Changes the language of menus, error messages, and labels without restarting.",
        [UiTextKeys.SettingsLanguageUk] = "Ukrainian (Українська)",
        [UiTextKeys.SettingsLanguageEn] = "English",
        [UiTextKeys.SettingsThemeLabel] = "Color Theme",
        [UiTextKeys.SettingsThemeDescription] = "Switches between light and dark window appearance.",
        [UiTextKeys.SettingsThemeLight] = "Light",
        [UiTextKeys.SettingsThemeDark] = "Dark",
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