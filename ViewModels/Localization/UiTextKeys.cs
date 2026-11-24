namespace AttendanceJournal.ViewModels.Localization;

public static class UiTextKeys
{
    // Діалоги підтвердження
    public const string ConfirmTitle = "Ui_ConfirmTitle";
    public const string ConfirmYes = "Ui_ConfirmYes";
    public const string ConfirmNo = "Ui_ConfirmNo";
    public const string ConfirmArchiveGroup = "Ui_ConfirmArchiveGroup";
    public const string ConfirmArchiveStudent = "Ui_ConfirmArchiveStudent";

    // Головне вікно та навігація
    public const string AppTitle = "Ui_AppTitle";
    public const string NavHeader = "Ui_NavHeader";
    public const string NavGroups = "Ui_NavGroups";
    public const string NavSettings = "Ui_NavSettings";
    public const string RetryButton = "Ui_RetryButton";

    // Екран «Групи та студенти»
    public const string GroupsHeader = "Ui_GroupsHeader";
    public const string ArchiveFilter = "Ui_ArchiveFilter";
    public const string GroupNamePlaceholder = "Ui_GroupNamePlaceholder";
    public const string AcademicYearPlaceholder = "Ui_AcademicYearPlaceholder";
    public const string SaveButton = "Ui_SaveButton";
    public const string CancelButton = "Ui_CancelButton";
    public const string EditButton = "Ui_EditButton";
    public const string ArchiveRestoreButton = "Ui_ArchiveRestoreButton";
    public const string ArchivedBadge = "Ui_ArchivedBadge";

    public const string StudentsHeader = "Ui_StudentsHeader";
    public const string ShowArchivedStudentsFilter = "Ui_ShowArchivedStudentsFilter";
    public const string StudentFullNamePlaceholder = "Ui_StudentFullNamePlaceholder";
    public const string SaveStudentButton = "Ui_SaveStudentButton";

    // Екран «Налаштування» (FR-13)
    public const string SettingsHeader = "Ui_SettingsHeader";
    public const string SettingsLanguageLabel = "Ui_SettingsLanguageLabel";
    public const string SettingsLanguageDescription = "Ui_SettingsLanguageDescription";
    public const string SettingsLanguageUk = "Ui_SettingsLanguageUk";
    public const string SettingsLanguageEn = "Ui_SettingsLanguageEn";
    public const string SettingsThemeLabel = "Ui_SettingsThemeLabel";
    public const string SettingsThemeDescription = "Ui_SettingsThemeDescription";
    public const string SettingsThemeLight = "Ui_SettingsThemeLight";
    public const string SettingsThemeDark = "Ui_SettingsThemeDark";

    // Навігація (нові екрани)
    public const string NavJournal = "Ui_NavJournal";
    public const string NavMonthGrid = "Ui_NavMonthGrid";
    public const string NavSummary = "Ui_NavSummary";
    public const string NavSubjects = "Ui_NavSubjects";
    public const string NavAuditLog = "Ui_NavAuditLog";

    // Спільні підписи
    public const string LabelGroup = "Ui_LabelGroup";
    public const string LabelSubject = "Ui_LabelSubject";
    public const string LabelDate = "Ui_LabelDate";
    public const string LabelPair = "Ui_LabelPair";
    public const string LabelTopic = "Ui_LabelTopic";
    public const string LabelFrom = "Ui_LabelFrom";
    public const string LabelTo = "Ui_LabelTo";
    public const string AllSubjects = "Ui_AllSubjects";
    public const string RefreshButton = "Ui_RefreshButton";
    public const string CloseButton = "Ui_CloseButton";
    public const string ShowArchivedGroupsOption = "Ui_ShowArchivedGroupsOption";
    public const string NoValue = "Ui_NoValue";

    // Дисципліни (FR-03)
    public const string SubjectNamePlaceholder = "Ui_SubjectNamePlaceholder";
    public const string ConfirmArchiveSubject = "Ui_ConfirmArchiveSubject";

    // Статуси відвідування (NFR-04: текст, а не лише колір)
    public const string StatusPresent = "Ui_StatusPresent";
    public const string StatusAbsent = "Ui_StatusAbsent";
    public const string StatusLate = "Ui_StatusLate";
    public const string StatusExcused = "Ui_StatusExcused";
    public const string StatusNotMarked = "Ui_StatusNotMarked";
    public const string LetterPresent = "Ui_LetterPresent";
    public const string LetterAbsent = "Ui_LetterAbsent";
    public const string LetterLate = "Ui_LetterLate";
    public const string LetterExcused = "Ui_LetterExcused";
    public const string LetterNotMarked = "Ui_LetterNotMarked";

    // Заголовки колонок (також у файлах експорту)
    public const string ColumnGroup = "Ui_ColumnGroup";
    public const string ColumnStudent = "Ui_ColumnStudent";
    public const string ColumnDate = "Ui_ColumnDate";
    public const string ColumnPair = "Ui_ColumnPair";
    public const string ColumnSubject = "Ui_ColumnSubject";
    public const string ColumnStatus = "Ui_ColumnStatus";
    public const string ColumnGrade = "Ui_ColumnGrade";
    public const string ColumnComment = "Ui_ColumnComment";
    public const string ColumnRate = "Ui_ColumnRate";
    public const string ColumnGradesCount = "Ui_ColumnGradesCount";
    public const string ColumnAverage = "Ui_ColumnAverage";

    // Журнал занять (FR-04 – FR-07, FR-16)
    public const string JournalHeader = "Ui_JournalHeader";
    public const string LessonsOnDateHeader = "Ui_LessonsOnDateHeader";
    public const string NoLessonsOnDate = "Ui_NoLessonsOnDate";
    public const string NewLessonHeader = "Ui_NewLessonHeader";
    public const string TopicPlaceholder = "Ui_TopicPlaceholder";
    public const string CreateLessonButton = "Ui_CreateLessonButton";
    public const string SaveTopicButton = "Ui_SaveTopicButton";
    public const string SelectLessonHint = "Ui_SelectLessonHint";
    public const string MarkAllPresentButton = "Ui_MarkAllPresentButton";
    public const string SaveMarksButton = "Ui_SaveMarksButton";
    public const string DiscardChangesButton = "Ui_DiscardChangesButton";
    public const string GradePlaceholder = "Ui_GradePlaceholder";
    public const string CommentPlaceholder = "Ui_CommentPlaceholder";
    public const string ReasonLabel = "Ui_ReasonLabel";
    public const string ReasonPlaceholder = "Ui_ReasonPlaceholder";
    public const string ShortcutsHint = "Ui_ShortcutsHint";
    public const string UnsavedChangesCount = "Ui_UnsavedChangesCount";
    public const string SavedMarksInfo = "Ui_SavedMarksInfo";
    public const string MarkedAllPresentInfo = "Ui_MarkedAllPresentInfo";
    public const string LessonCreatedInfo = "Ui_LessonCreatedInfo";
    public const string TopicSavedInfo = "Ui_TopicSavedInfo";
    public const string ConfirmDiscardChanges = "Ui_ConfirmDiscardChanges";
    public const string HistoryButton = "Ui_HistoryButton";
    public const string HistoryHeader = "Ui_HistoryHeader";
    public const string HistoryVersion = "Ui_HistoryVersion";
    public const string HistoryReason = "Ui_HistoryReason";
    public const string RestoreButton = "Ui_RestoreButton";
    public const string RestoreReasonDefault = "Ui_RestoreReasonDefault";
    public const string RestoredInfo = "Ui_RestoredInfo";

    // Місячна сітка (FR-08)
    public const string PrevMonthButton = "Ui_PrevMonthButton";
    public const string NextMonthButton = "Ui_NextMonthButton";
    public const string MonthGridLegend = "Ui_MonthGridLegend";
    public const string NoLessonsInMonth = "Ui_NoLessonsInMonth";

    // Підсумки та експорт (FR-09, FR-10)
    public const string ShowButton = "Ui_ShowButton";
    public const string IncludeArchivedStudents = "Ui_IncludeArchivedStudents";
    public const string GroupTotalsLine = "Ui_GroupTotalsLine";
    public const string AverageNeedsGrades = "Ui_AverageNeedsGrades";
    public const string GroupTotalRow = "Ui_GroupTotalRow";
    public const string ExportCsvButton = "Ui_ExportCsvButton";
    public const string ExportXlsxButton = "Ui_ExportXlsxButton";
    public const string ExportRowsCount = "Ui_ExportRowsCount";
    public const string ExportDoneInfo = "Ui_ExportDoneInfo";
    public const string ExportDialogTitle = "Ui_ExportDialogTitle";
    public const string AttendanceSheetName = "Ui_AttendanceSheetName";
    public const string SummarySheetName = "Ui_SummarySheetName";

    // Журнал змін (FR-12)
    public const string AllEntityTypes = "Ui_AllEntityTypes";
    public const string AllActions = "Ui_AllActions";
    public const string AuditColumnTime = "Ui_AuditColumnTime";
    public const string AuditColumnAction = "Ui_AuditColumnAction";
    public const string AuditColumnEntity = "Ui_AuditColumnEntity";
    public const string AuditColumnId = "Ui_AuditColumnId";
    public const string AuditColumnDetails = "Ui_AuditColumnDetails";
    public const string AuditShownCount = "Ui_AuditShownCount";
    public const string AuditReadOnlyNote = "Ui_AuditReadOnlyNote";
    public const string ActionCreate = "Ui_ActionCreate";
    public const string ActionUpdate = "Ui_ActionUpdate";
    public const string ActionArchive = "Ui_ActionArchive";
    public const string ActionUnarchive = "Ui_ActionUnarchive";
    public const string ActionMarkAttendance = "Ui_ActionMarkAttendance";
    public const string ActionBulkImport = "Ui_ActionBulkImport";
    public const string ActionRestoreVersion = "Ui_ActionRestoreVersion";
    public const string EntityStudentGroup = "Ui_EntityStudentGroup";
    public const string EntityStudent = "Ui_EntityStudent";
    public const string EntitySubject = "Ui_EntitySubject";
    public const string EntityLesson = "Ui_EntityLesson";
    public const string EntityAttendanceRecord = "Ui_EntityAttendanceRecord";
    public const string EntitySetting = "Ui_EntitySetting";

    // Імпорт студентів (FR-11)
    public const string ImportButton = "Ui_ImportButton";
    public const string ImportDialogTitle = "Ui_ImportDialogTitle";
    public const string ImportFileTypeName = "Ui_ImportFileTypeName";
    public const string ImportHint = "Ui_ImportHint";
    public const string ImportPreviewHeader = "Ui_ImportPreviewHeader";
    public const string ImportPreviewCounts = "Ui_ImportPreviewCounts";
    public const string ImportColumnRow = "Ui_ImportColumnRow";
    public const string ImportColumnResult = "Ui_ImportColumnResult";
    public const string ImportStatusValid = "Ui_ImportStatusValid";
    public const string ImportStatusNewGroup = "Ui_ImportStatusNewGroup";
    public const string ImportConfirmButton = "Ui_ImportConfirmButton";
    public const string ImportConfirmNewGroups = "Ui_ImportConfirmNewGroups";
    public const string ImportDoneInfo = "Ui_ImportDoneInfo";

    // Додаткові повідомлення екранів
    public const string RowStatusRequired = "Ui_RowStatusRequired";
    public const string FixRowErrors = "Ui_FixRowErrors";
    public const string ArchivedSuffix = "Ui_ArchivedSuffix";
    public const string CurrentVersionBadge = "Ui_CurrentVersionBadge";
    public const string ClearFiltersButton = "Ui_ClearFiltersButton";

    // Повторювані заняття
    public const string RepeatLessonOption = "Ui_RepeatLessonOption";
    public const string LabelDayOfWeek = "Ui_LabelDayOfWeek";
    public const string LabelRepeat = "Ui_LabelRepeat";
    public const string RepeatEveryWeek = "Ui_RepeatEveryWeek";
    public const string RepeatEveryTwoWeeks = "Ui_RepeatEveryTwoWeeks";
    public const string RepeatEveryThreeWeeks = "Ui_RepeatEveryThreeWeeks";
    public const string RepeatEveryFourWeeks = "Ui_RepeatEveryFourWeeks";
    public const string SeriesHint = "Ui_SeriesHint";
    public const string CreateSeriesButton = "Ui_CreateSeriesButton";
    public const string ConfirmCreateSeries = "Ui_ConfirmCreateSeries";
    public const string SeriesCreatedInfo = "Ui_SeriesCreatedInfo";
    public const string ActionCreateSeries = "Ui_ActionCreateSeries";
}
