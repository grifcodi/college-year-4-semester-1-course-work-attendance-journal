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

        [ErrorKeys.AttendanceDuplicateStudentInBatch] = "Один студент потрапив у збереження двічі. Оновіть заняття і спробуйте ще раз.",
        [ErrorKeys.ImportUnsupportedFormat] = "Непідтримуваний формат файлу. Оберіть файл .csv або .xlsx.",
        [ErrorKeys.ImportFileEmpty] = "Файл порожній або не містить рядків зі студентами. Додайте рядки під заголовком.",
        [ErrorKeys.ImportFileNotUtf8] = "Файл CSV не в кодуванні UTF-8. Збережіть його в Excel як «CSV UTF-8» і повторіть імпорт.",
        [ErrorKeys.ImportFileUnreadable] = "Не вдалося прочитати файл: він пошкоджений або має інший формат. Відкрийте його в Excel, збережіть ще раз і повторіть імпорт.",
        [ErrorKeys.ImportMissingFullNameColumn] = "У першому рядку файлу немає колонки FullName (або ПІБ). Додайте заголовок і повторіть імпорт.",
        [ErrorKeys.ImportNoTargetGroup] = "Не вказано групу: заповніть колонку Group або оберіть групу в списку перед імпортом.",
        [ErrorKeys.ImportNothingToImport] = "Немає рядків для імпорту: усі рядки — дублікати, мають помилки або належать до нових груп, створення яких скасовано.",
        [ErrorKeys.ImportRowNameRequired] = "Порожнє ПІБ. Заповніть ПІБ у цьому рядку.",
        [ErrorKeys.ImportRowNameTooLong] = "ПІБ довше за {0} символів. Скоротіть його у файлі.",
        [ErrorKeys.ImportRowGroupNameTooLong] = "Назва групи довша за {0} символів. Скоротіть її у файлі.",
        [ErrorKeys.ImportRowDuplicateInFile] = "Повторює рядок {0} цього файлу; пропущено.",
        [ErrorKeys.ImportRowDuplicateInDatabase] = "Студент «{0}» уже є в цій групі; пропущено.",
        [ErrorKeys.ExportInvalidRange] = "Початкова дата пізніша за кінцеву. Виправте діапазон дат.",
        [ErrorKeys.ExportWriteFailed] = "Не вдалося записати файл. Перевірте, чи він не відкритий в іншій програмі, і спробуйте ще раз.",

        // Навігація (нові екрани)
        [UiTextKeys.NavJournal] = "Журнал занять",
        [UiTextKeys.NavMonthGrid] = "Місячна сітка",
        [UiTextKeys.NavSummary] = "Підсумки та експорт",
        [UiTextKeys.NavSubjects] = "Дисципліни",
        [UiTextKeys.NavAuditLog] = "Журнал змін",

        // Спільні підписи
        [UiTextKeys.LabelGroup] = "Група:",
        [UiTextKeys.LabelSubject] = "Дисципліна:",
        [UiTextKeys.LabelDate] = "Дата:",
        [UiTextKeys.LabelPair] = "Пара:",
        [UiTextKeys.LabelTopic] = "Тема:",
        [UiTextKeys.LabelFrom] = "З:",
        [UiTextKeys.LabelTo] = "По:",
        [UiTextKeys.AllSubjects] = "Усі дисципліни",
        [UiTextKeys.RefreshButton] = "Оновити",
        [UiTextKeys.CloseButton] = "Закрити",
        [UiTextKeys.ShowArchivedGroupsOption] = "Показувати архівні групи",
        [UiTextKeys.NoValue] = "—",

        // Дисципліни (FR-03)
        [UiTextKeys.SubjectNamePlaceholder] = "Назва дисципліни (напр. Бази даних)",
        [UiTextKeys.ConfirmArchiveSubject] = "Заархівувати дисципліну «{0}»? Вона зникне зі списків вибору, але її заняття та відмітки збережуться.",

        // Статуси відвідування (NFR-04: текст, а не лише колір)
        [UiTextKeys.StatusPresent] = "Присутній",
        [UiTextKeys.StatusAbsent] = "Відсутній",
        [UiTextKeys.StatusLate] = "Запізнився",
        [UiTextKeys.StatusExcused] = "Поважна причина",
        [UiTextKeys.StatusNotMarked] = "Не відмічено",
        [UiTextKeys.LetterPresent] = "П",
        [UiTextKeys.LetterAbsent] = "Н",
        [UiTextKeys.LetterLate] = "З",
        [UiTextKeys.LetterExcused] = "ПП",
        [UiTextKeys.LetterNotMarked] = "·",

        // Заголовки колонок (також у файлах експорту)
        [UiTextKeys.ColumnGroup] = "Група",
        [UiTextKeys.ColumnStudent] = "Студент",
        [UiTextKeys.ColumnDate] = "Дата",
        [UiTextKeys.ColumnPair] = "Пара",
        [UiTextKeys.ColumnSubject] = "Дисципліна",
        [UiTextKeys.ColumnStatus] = "Статус",
        [UiTextKeys.ColumnGrade] = "Оцінка",
        [UiTextKeys.ColumnComment] = "Коментар",
        [UiTextKeys.ColumnRate] = "Відвідуваність, %",
        [UiTextKeys.ColumnGradesCount] = "Кількість оцінок",
        [UiTextKeys.ColumnAverage] = "Середній бал",

        // Журнал занять (FR-04 – FR-07, FR-16)
        [UiTextKeys.JournalHeader] = "Заняття та відмітки",
        [UiTextKeys.LessonsOnDateHeader] = "Заняття на обрану дату",
        [UiTextKeys.NoLessonsOnDate] = "На цю дату занять немає. Створіть заняття нижче.",
        [UiTextKeys.NewLessonHeader] = "Нове заняття",
        [UiTextKeys.TopicPlaceholder] = "Тема (необов'язково)",
        [UiTextKeys.CreateLessonButton] = "Створити заняття",
        [UiTextKeys.SaveTopicButton] = "Зберегти тему",
        [UiTextKeys.SelectLessonHint] = "Оберіть або створіть заняття ліворуч, щоб відмітити присутність.",
        [UiTextKeys.MarkAllPresentButton] = "Усі присутні",
        [UiTextKeys.SaveMarksButton] = "Зберегти відмітки",
        [UiTextKeys.DiscardChangesButton] = "Скасувати зміни",
        [UiTextKeys.GradePlaceholder] = "0–100",
        [UiTextKeys.CommentPlaceholder] = "Коментар (необов'язково)",
        [UiTextKeys.ReasonLabel] = "Причина зміни збережених відміток (щонайменше 5 символів):",
        [UiTextKeys.ReasonPlaceholder] = "Напр.: виправлено помилку введення",
        [UiTextKeys.ShortcutsHint] = "Клавіатура: Tab — наступне поле; Ctrl+1…4 — статус рядка (П, Н, З, ПП); Ctrl+S — зберегти; Ctrl+M — усі присутні.",
        [UiTextKeys.UnsavedChangesCount] = "Незбережених змін: {0}",
        [UiTextKeys.SavedMarksInfo] = "Збережено відміток: {0}.",
        [UiTextKeys.MarkedAllPresentInfo] = "Позначено присутніми: {0}.",
        [UiTextKeys.LessonCreatedInfo] = "Заняття створено.",
        [UiTextKeys.TopicSavedInfo] = "Тему заняття збережено.",
        [UiTextKeys.ConfirmDiscardChanges] = "Є незбережені відмітки. Відкинути їх?",
        [UiTextKeys.HistoryButton] = "Історія",
        [UiTextKeys.HistoryHeader] = "Історія відмітки: {0}",
        [UiTextKeys.HistoryVersion] = "Версія {0}",
        [UiTextKeys.HistoryReason] = "Причина: {0}",
        [UiTextKeys.RestoreButton] = "Відновити цю версію",
        [UiTextKeys.RestoreReasonDefault] = "Відновлено версію {0}",
        [UiTextKeys.RestoredInfo] = "Відновлено версію {0} як нову версію.",

        // Місячна сітка (FR-08)
        [UiTextKeys.PrevMonthButton] = "◀ Попередній",
        [UiTextKeys.NextMonthButton] = "Наступний ▶",
        [UiTextKeys.MonthGridLegend] = "Позначення: П — присутній, Н — відсутній, З — запізнився, ПП — поважна причина, · — не відмічено. Клацніть клітинку (або Enter), щоб відкрити заняття.",
        [UiTextKeys.NoLessonsInMonth] = "У цьому місяці занять немає.",

        // Підсумки та експорт (FR-09, FR-10)
        [UiTextKeys.ShowButton] = "Показати",
        [UiTextKeys.IncludeArchivedStudents] = "Показати всіх архівних студентів",
        [UiTextKeys.GroupTotalsLine] = "Група загалом: відвідуваність {0}, середній бал {1} (студентів з 3+ оцінками: {2}); занять: {3}.",
        [UiTextKeys.AverageNeedsGrades] = "— ({0} оц.)",
        [UiTextKeys.GroupTotalRow] = "Група загалом",
        [UiTextKeys.ExportCsvButton] = "Експорт CSV",
        [UiTextKeys.ExportXlsxButton] = "Експорт XLSX",
        [UiTextKeys.ExportRowsCount] = "Рядків для експорту: {0}",
        [UiTextKeys.ExportDoneInfo] = "Експортовано рядків: {0} у файл {1}.",
        [UiTextKeys.ExportDialogTitle] = "Зберегти експорт відвідуваності",
        [UiTextKeys.AttendanceSheetName] = "Відвідуваність",
        [UiTextKeys.SummarySheetName] = "Підсумок",

        // Журнал змін (FR-12)
        [UiTextKeys.AllEntityTypes] = "Усі типи об'єктів",
        [UiTextKeys.AllActions] = "Усі дії",
        [UiTextKeys.AuditColumnTime] = "Час",
        [UiTextKeys.AuditColumnAction] = "Дія",
        [UiTextKeys.AuditColumnEntity] = "Об'єкт",
        [UiTextKeys.AuditColumnId] = "ID",
        [UiTextKeys.AuditColumnDetails] = "Подробиці",
        [UiTextKeys.AuditShownCount] = "Показано {0} з {1} записів (найновіші спочатку).",
        [UiTextKeys.AuditReadOnlyNote] = "Записи журналу змін не можна редагувати чи видаляти.",
        [UiTextKeys.ActionCreate] = "Створено",
        [UiTextKeys.ActionUpdate] = "Змінено",
        [UiTextKeys.ActionArchive] = "Заархівовано",
        [UiTextKeys.ActionUnarchive] = "Відновлено з архіву",
        [UiTextKeys.ActionMarkAttendance] = "Відмічено відвідування",
        [UiTextKeys.ActionBulkImport] = "Масовий імпорт",
        [UiTextKeys.ActionRestoreVersion] = "Відновлено версію відмітки",
        [UiTextKeys.EntityStudentGroup] = "Навчальна група",
        [UiTextKeys.EntityStudent] = "Здобувач освіти",
        [UiTextKeys.EntitySubject] = "Навчальна дисципліна",
        [UiTextKeys.EntityLesson] = "Заняття",
        [UiTextKeys.EntityAttendanceRecord] = "Відмітка відвідування",
        [UiTextKeys.EntitySetting] = "Параметр інтерфейсу",

        // Імпорт студентів (FR-11)
        [UiTextKeys.ImportButton] = "Імпорт студентів…",
        [UiTextKeys.ImportDialogTitle] = "Оберіть файл CSV або XLSX зі студентами",
        [UiTextKeys.ImportFileTypeName] = "Таблиці зі студентами (CSV, XLSX)",
        [UiTextKeys.ImportHint] = "Перший рядок — заголовки. Обов'язкова колонка FullName (або ПІБ), необов'язкова Group (або Група). CSV — у кодуванні UTF-8, роздільник «,» або «;». Рядки без групи потрапляють в обрану ліворуч групу.",
        [UiTextKeys.ImportPreviewHeader] = "Перегляд імпорту: {0}",
        [UiTextKeys.ImportPreviewCounts] = "Буде додано: {0}; дублікати (пропущено): {1}; з помилками (пропущено): {2}.",
        [UiTextKeys.ImportColumnRow] = "Рядок",
        [UiTextKeys.ImportColumnResult] = "Результат",
        [UiTextKeys.ImportStatusValid] = "Буде додано",
        [UiTextKeys.ImportStatusNewGroup] = "Буде додано в нову групу",
        [UiTextKeys.ImportConfirmButton] = "Імпортувати ({0})",
        [UiTextKeys.ImportConfirmNewGroups] = "У файлі є групи, яких ще немає: {0}. Створити їх з навчальним роком {1}? Якщо ні, рядки цих груп буде пропущено.",
        [UiTextKeys.ImportDoneInfo] = "Імпорт завершено: додано студентів — {0}, створено груп — {1}, пропущено дублікатів — {2}, рядків з помилками — {3}.",
        [UiTextKeys.RowStatusRequired] = "Оберіть статус: без нього оцінку чи коментар не збережено.",
        [UiTextKeys.FixRowErrors] = "Виправте позначені рядки й спробуйте ще раз.",
        [UiTextKeys.ArchivedSuffix] = "{0} (архів)",
        [UiTextKeys.CurrentVersionBadge] = "поточна",
        [UiTextKeys.ClearFiltersButton] = "Скинути фільтри",
        [ErrorKeys.LessonSeriesInvalidRange] = "Дата початку серії пізніша за дату завершення. Виправте дати.",
        [ErrorKeys.LessonSeriesTooLong] = "Серія може тривати не довше одного року. Скоротіть діапазон дат.",
        [ErrorKeys.LessonSeriesInvalidInterval] = "Повторення має бути від 1 до {0} тижнів.",
        [ErrorKeys.LessonSeriesNoDates] = "У вибраних датах немає жодного такого дня тижня. Змініть дати або день тижня.",
        [ErrorKeys.LessonSeriesAllTaken] = "На всі дати серії ця пара вже зайнята. Оберіть іншу пару або інші дати.",
        [ErrorKeys.LessonSeriesConflict] = "Деякі дати серії щойно зайняли інші заняття. Спробуйте створити серію ще раз.",
        [UiTextKeys.RepeatLessonOption] = "Повторюване заняття (серія)",
        [UiTextKeys.LabelDayOfWeek] = "День тижня:",
        [UiTextKeys.LabelRepeat] = "Повторення:",
        [UiTextKeys.RepeatEveryWeek] = "Щотижня",
        [UiTextKeys.RepeatEveryTwoWeeks] = "Раз на два тижні (чисельник / знаменник)",
        [UiTextKeys.RepeatEveryThreeWeeks] = "Раз на три тижні",
        [UiTextKeys.RepeatEveryFourWeeks] = "Раз на чотири тижні",
        [UiTextKeys.SeriesHint] = "Заняття створюються для обраної групи, дисципліни й пари на кожен обраний день тижня між датами. Дати, де ця пара вже зайнята, пропускаються.",
        [UiTextKeys.CreateSeriesButton] = "Створити серію занять",
        [UiTextKeys.ConfirmCreateSeries] = "Буде створено занять: {0} ({1}, пара {2}) з {3} по {4}. Дат, де пара вже зайнята і які буде пропущено: {5}. Заняття не можна видалити після створення. Продовжити?",
        [UiTextKeys.SeriesCreatedInfo] = "Створено занять: {0}. Пропущено зайнятих дат: {1}.",
        [UiTextKeys.ActionCreateSeries] = "Створено серію занять",
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

        [ErrorKeys.AttendanceDuplicateStudentInBatch] = "The same student appears twice in one save. Reload the lesson and try again.",
        [ErrorKeys.ImportUnsupportedFormat] = "Unsupported file format. Choose a .csv or .xlsx file.",
        [ErrorKeys.ImportFileEmpty] = "The file is empty or has no student rows. Add rows below the header.",
        [ErrorKeys.ImportFileNotUtf8] = "The CSV file is not UTF-8. Save it in Excel as “CSV UTF-8” and import again.",
        [ErrorKeys.ImportFileUnreadable] = "The file could not be read: it is damaged or in another format. Open it in Excel, save it again and retry the import.",
        [ErrorKeys.ImportMissingFullNameColumn] = "The first row of the file has no FullName column. Add the header and import again.",
        [ErrorKeys.ImportNoTargetGroup] = "No group given: fill in the Group column or select a group in the list before importing.",
        [ErrorKeys.ImportNothingToImport] = "Nothing to import: all rows are duplicates, have errors, or belong to new groups that you chose not to create.",
        [ErrorKeys.ImportRowNameRequired] = "The full name is empty. Fill it in on this row.",
        [ErrorKeys.ImportRowNameTooLong] = "The full name is longer than {0} characters. Shorten it in the file.",
        [ErrorKeys.ImportRowGroupNameTooLong] = "The group name is longer than {0} characters. Shorten it in the file.",
        [ErrorKeys.ImportRowDuplicateInFile] = "Repeats row {0} of this file; skipped.",
        [ErrorKeys.ImportRowDuplicateInDatabase] = "Student “{0}” is already in this group; skipped.",
        [ErrorKeys.ExportInvalidRange] = "The start date is after the end date. Correct the date range.",
        [ErrorKeys.ExportWriteFailed] = "The file could not be written. Make sure it is not open in another program and try again.",

        // Навігація (нові екрани)
        [UiTextKeys.NavJournal] = "Lesson journal",
        [UiTextKeys.NavMonthGrid] = "Month grid",
        [UiTextKeys.NavSummary] = "Summary & export",
        [UiTextKeys.NavSubjects] = "Subjects",
        [UiTextKeys.NavAuditLog] = "Change log",

        // Спільні підписи
        [UiTextKeys.LabelGroup] = "Group:",
        [UiTextKeys.LabelSubject] = "Subject:",
        [UiTextKeys.LabelDate] = "Date:",
        [UiTextKeys.LabelPair] = "Pair:",
        [UiTextKeys.LabelTopic] = "Topic:",
        [UiTextKeys.LabelFrom] = "From:",
        [UiTextKeys.LabelTo] = "To:",
        [UiTextKeys.AllSubjects] = "All subjects",
        [UiTextKeys.RefreshButton] = "Refresh",
        [UiTextKeys.CloseButton] = "Close",
        [UiTextKeys.ShowArchivedGroupsOption] = "Show archived groups",
        [UiTextKeys.NoValue] = "—",

        // Дисципліни (FR-03)
        [UiTextKeys.SubjectNamePlaceholder] = "Subject name (e.g. Databases)",
        [UiTextKeys.ConfirmArchiveSubject] = "Archive subject “{0}”? It will disappear from the pickers, but its lessons and marks are kept.",

        // Статуси відвідування (NFR-04: текст, а не лише колір)
        [UiTextKeys.StatusPresent] = "Present",
        [UiTextKeys.StatusAbsent] = "Absent",
        [UiTextKeys.StatusLate] = "Late",
        [UiTextKeys.StatusExcused] = "Excused",
        [UiTextKeys.StatusNotMarked] = "Not marked",
        [UiTextKeys.LetterPresent] = "P",
        [UiTextKeys.LetterAbsent] = "A",
        [UiTextKeys.LetterLate] = "L",
        [UiTextKeys.LetterExcused] = "E",
        [UiTextKeys.LetterNotMarked] = "·",

        // Заголовки колонок (також у файлах експорту)
        [UiTextKeys.ColumnGroup] = "Group",
        [UiTextKeys.ColumnStudent] = "Student",
        [UiTextKeys.ColumnDate] = "Date",
        [UiTextKeys.ColumnPair] = "Pair",
        [UiTextKeys.ColumnSubject] = "Subject",
        [UiTextKeys.ColumnStatus] = "Status",
        [UiTextKeys.ColumnGrade] = "Grade",
        [UiTextKeys.ColumnComment] = "Comment",
        [UiTextKeys.ColumnRate] = "Attendance, %",
        [UiTextKeys.ColumnGradesCount] = "Number of grades",
        [UiTextKeys.ColumnAverage] = "Average grade",

        // Журнал занять (FR-04 – FR-07, FR-16)
        [UiTextKeys.JournalHeader] = "Lessons and marks",
        [UiTextKeys.LessonsOnDateHeader] = "Lessons on the selected date",
        [UiTextKeys.NoLessonsOnDate] = "There are no lessons on this date. Create one below.",
        [UiTextKeys.NewLessonHeader] = "New lesson",
        [UiTextKeys.TopicPlaceholder] = "Topic (optional)",
        [UiTextKeys.CreateLessonButton] = "Create lesson",
        [UiTextKeys.SaveTopicButton] = "Save topic",
        [UiTextKeys.SelectLessonHint] = "Select or create a lesson on the left to mark attendance.",
        [UiTextKeys.MarkAllPresentButton] = "Mark all present",
        [UiTextKeys.SaveMarksButton] = "Save marks",
        [UiTextKeys.DiscardChangesButton] = "Discard changes",
        [UiTextKeys.GradePlaceholder] = "0–100",
        [UiTextKeys.CommentPlaceholder] = "Comment (optional)",
        [UiTextKeys.ReasonLabel] = "Reason for changing saved marks (at least 5 characters):",
        [UiTextKeys.ReasonPlaceholder] = "E.g.: corrected a typing error",
        [UiTextKeys.ShortcutsHint] = "Keyboard: Tab — next field; Ctrl+1…4 — row status (P, A, L, E); Ctrl+S — save; Ctrl+M — mark all present.",
        [UiTextKeys.UnsavedChangesCount] = "Unsaved changes: {0}",
        [UiTextKeys.SavedMarksInfo] = "Marks saved: {0}.",
        [UiTextKeys.MarkedAllPresentInfo] = "Marked present: {0}.",
        [UiTextKeys.LessonCreatedInfo] = "Lesson created.",
        [UiTextKeys.TopicSavedInfo] = "Lesson topic saved.",
        [UiTextKeys.ConfirmDiscardChanges] = "There are unsaved marks. Discard them?",
        [UiTextKeys.HistoryButton] = "History",
        [UiTextKeys.HistoryHeader] = "Mark history: {0}",
        [UiTextKeys.HistoryVersion] = "Version {0}",
        [UiTextKeys.HistoryReason] = "Reason: {0}",
        [UiTextKeys.RestoreButton] = "Restore this version",
        [UiTextKeys.RestoreReasonDefault] = "Restored version {0}",
        [UiTextKeys.RestoredInfo] = "Version {0} was restored as a new version.",

        // Місячна сітка (FR-08)
        [UiTextKeys.PrevMonthButton] = "◀ Previous",
        [UiTextKeys.NextMonthButton] = "Next ▶",
        [UiTextKeys.MonthGridLegend] = "Legend: P — present, A — absent, L — late, E — excused, · — not marked. Click a cell (or press Enter) to open the lesson.",
        [UiTextKeys.NoLessonsInMonth] = "There are no lessons in this month.",

        // Підсумки та експорт (FR-09, FR-10)
        [UiTextKeys.ShowButton] = "Show",
        [UiTextKeys.IncludeArchivedStudents] = "Show all archived students",
        [UiTextKeys.GroupTotalsLine] = "Group total: attendance {0}, average grade {1} (students with 3+ grades: {2}); lessons: {3}.",
        [UiTextKeys.AverageNeedsGrades] = "— ({0} gr.)",
        [UiTextKeys.GroupTotalRow] = "Group total",
        [UiTextKeys.ExportCsvButton] = "Export CSV",
        [UiTextKeys.ExportXlsxButton] = "Export XLSX",
        [UiTextKeys.ExportRowsCount] = "Rows to export: {0}",
        [UiTextKeys.ExportDoneInfo] = "Exported {0} rows to {1}.",
        [UiTextKeys.ExportDialogTitle] = "Save attendance export",
        [UiTextKeys.AttendanceSheetName] = "Attendance",
        [UiTextKeys.SummarySheetName] = "Summary",

        // Журнал змін (FR-12)
        [UiTextKeys.AllEntityTypes] = "All object types",
        [UiTextKeys.AllActions] = "All actions",
        [UiTextKeys.AuditColumnTime] = "Time",
        [UiTextKeys.AuditColumnAction] = "Action",
        [UiTextKeys.AuditColumnEntity] = "Object",
        [UiTextKeys.AuditColumnId] = "ID",
        [UiTextKeys.AuditColumnDetails] = "Details",
        [UiTextKeys.AuditShownCount] = "Showing {0} of {1} entries (newest first).",
        [UiTextKeys.AuditReadOnlyNote] = "Change log entries cannot be edited or deleted.",
        [UiTextKeys.ActionCreate] = "Created",
        [UiTextKeys.ActionUpdate] = "Updated",
        [UiTextKeys.ActionArchive] = "Archived",
        [UiTextKeys.ActionUnarchive] = "Restored from archive",
        [UiTextKeys.ActionMarkAttendance] = "Attendance marked",
        [UiTextKeys.ActionBulkImport] = "Bulk import",
        [UiTextKeys.ActionRestoreVersion] = "Mark version restored",
        [UiTextKeys.EntityStudentGroup] = "Student group",
        [UiTextKeys.EntityStudent] = "Student record",
        [UiTextKeys.EntitySubject] = "Course subject",
        [UiTextKeys.EntityLesson] = "Lesson",
        [UiTextKeys.EntityAttendanceRecord] = "Attendance mark",
        [UiTextKeys.EntitySetting] = "Interface setting",

        // Імпорт студентів (FR-11)
        [UiTextKeys.ImportButton] = "Import students…",
        [UiTextKeys.ImportDialogTitle] = "Choose a CSV or XLSX file with students",
        [UiTextKeys.ImportFileTypeName] = "Student tables (CSV, XLSX)",
        [UiTextKeys.ImportHint] = "The first row holds the headers. Column FullName is required, Group is optional. CSV must be UTF-8 with ',' or ';' as the delimiter. Rows without a group go to the group selected on the left.",
        [UiTextKeys.ImportPreviewHeader] = "Import preview: {0}",
        [UiTextKeys.ImportPreviewCounts] = "To add: {0}; duplicates (skipped): {1}; with errors (skipped): {2}.",
        [UiTextKeys.ImportColumnRow] = "Row",
        [UiTextKeys.ImportColumnResult] = "Result",
        [UiTextKeys.ImportStatusValid] = "Will be added",
        [UiTextKeys.ImportStatusNewGroup] = "Will be added to a new group",
        [UiTextKeys.ImportConfirmButton] = "Import ({0})",
        [UiTextKeys.ImportConfirmNewGroups] = "The file has groups that do not exist yet: {0}. Create them with academic year {1}? If not, the rows of these groups are skipped.",
        [UiTextKeys.ImportDoneInfo] = "Import finished: {0} students added, {1} groups created, {2} duplicates skipped, {3} rows with errors.",
        [UiTextKeys.RowStatusRequired] = "Choose a status: a grade or comment cannot be saved without it.",
        [UiTextKeys.FixRowErrors] = "Fix the highlighted rows and try again.",
        [UiTextKeys.ArchivedSuffix] = "{0} (archived)",
        [UiTextKeys.CurrentVersionBadge] = "current",
        [UiTextKeys.ClearFiltersButton] = "Clear filters",
        [ErrorKeys.LessonSeriesInvalidRange] = "The series start date is after its end date. Correct the dates.",
        [ErrorKeys.LessonSeriesTooLong] = "A series can last at most one year. Shorten the date range.",
        [ErrorKeys.LessonSeriesInvalidInterval] = "The repeat interval must be 1 to {0} weeks.",
        [ErrorKeys.LessonSeriesNoDates] = "There is no such weekday between the chosen dates. Change the dates or the weekday.",
        [ErrorKeys.LessonSeriesAllTaken] = "This pair is already taken on every date of the series. Choose another pair or other dates.",
        [ErrorKeys.LessonSeriesConflict] = "Some dates of the series were just taken by other lessons. Try creating the series again.",
        [UiTextKeys.RepeatLessonOption] = "Repeating lesson (series)",
        [UiTextKeys.LabelDayOfWeek] = "Day of week:",
        [UiTextKeys.LabelRepeat] = "Repeat:",
        [UiTextKeys.RepeatEveryWeek] = "Every week",
        [UiTextKeys.RepeatEveryTwoWeeks] = "Every two weeks",
        [UiTextKeys.RepeatEveryThreeWeeks] = "Every three weeks",
        [UiTextKeys.RepeatEveryFourWeeks] = "Every four weeks",
        [UiTextKeys.SeriesHint] = "Lessons are created for the selected group, subject and pair on every chosen weekday between the dates. Dates where this pair is already taken are skipped.",
        [UiTextKeys.CreateSeriesButton] = "Create lesson series",
        [UiTextKeys.ConfirmCreateSeries] = "{0} lessons will be created ({1}, pair {2}) from {3} to {4}. Dates skipped because the pair is already taken: {5}. Lessons cannot be deleted once created. Continue?",
        [UiTextKeys.SeriesCreatedInfo] = "Lessons created: {0}. Taken dates skipped: {1}.",
        [UiTextKeys.ActionCreateSeries] = "Lesson series created",
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
