namespace AttendanceJournal.Services.Common;

public static class ErrorKeys
{
    public const string DatabaseUnavailable = "Err_Database_Unavailable";
    public const string DatabaseConflict = "Err_Database_Conflict";
    public const string EntityNotFound = "Err_Entity_NotFound";
    public const string DatabaseBusy = "Err_Database_Busy";


    public const string SubjectNameRequired = "Err_Subject_NameRequired";
    public const string SubjectNameTooLong = "Err_Subject_NameTooLong";
    public const string SubjectAlreadyExists = "Err_Subject_AlreadyExists";

    public const string GroupNameRequired = "Err_Group_NameRequired";
    public const string GroupNameTooLong = "Err_Group_NameTooLong";
    public const string GroupYearRequired = "Err_Group_YearRequired";
    public const string GroupYearTooLong = "Err_Group_YearTooLong";
    public const string GroupAlreadyExists = "Err_Group_AlreadyExists";
    public const string GroupUnarchiveConflict = "Err_Group_UnarchiveConflict";

    public const string StudentNameRequired = "Err_Student_NameRequired";
    public const string StudentNameTooLong = "Err_Student_NameTooLong";
    public const string StudentAlreadyExists = "Err_Student_AlreadyExists";
    public const string StudentGroupArchived = "Err_Student_GroupArchived";

    public const string LessonInvalidPairNumber = "Err_Lesson_InvalidPairNumber";
    public const string LessonTopicTooLong = "Err_Lesson_TopicTooLong";
    public const string LessonDuplicateSlot = "Err_Lesson_DuplicateSlot";

    public const string AttendanceInvalidGrade = "Err_Attendance_InvalidGrade";
    public const string AttendanceNoteTooLong = "Err_Attendance_NoteTooLong";
    public const string AttendanceReasonRequired = "Err_Attendance_ReasonRequired";
    public const string AttendanceReasonTooShort = "Err_Attendance_ReasonTooShort";
    public const string AttendanceReasonTooLong = "Err_Attendance_ReasonTooLong";
    public const string AttendanceStudentArchived = "Err_Attendance_StudentArchived";
    public const string AttendanceStudentWrongGroup = "Err_Attendance_StudentWrongGroup";
    public const string AttendanceVersionNotFound = "Err_Attendance_VersionNotFound";
    public const string AttendanceNoChange = "Err_Attendance_NoChange";
    public const string AttendanceDuplicateStudentInBatch = "Err_Attendance_DuplicateStudentInBatch";

    // Import of students (FR-11)
    public const string ImportUnsupportedFormat = "Err_Import_UnsupportedFormat";
    public const string ImportFileEmpty = "Err_Import_FileEmpty";
    public const string ImportFileNotUtf8 = "Err_Import_FileNotUtf8";
    public const string ImportFileUnreadable = "Err_Import_FileUnreadable";
    public const string ImportMissingFullNameColumn = "Err_Import_MissingFullNameColumn";
    public const string ImportNoTargetGroup = "Err_Import_NoTargetGroup";
    public const string ImportNothingToImport = "Err_Import_NothingToImport";
    public const string ImportRowNameRequired = "Err_Import_RowNameRequired";
    public const string ImportRowNameTooLong = "Err_Import_RowNameTooLong";
    public const string ImportRowGroupNameTooLong = "Err_Import_RowGroupNameTooLong";
    public const string ImportRowDuplicateInFile = "Err_Import_RowDuplicateInFile";
    public const string ImportRowDuplicateInDatabase = "Err_Import_RowDuplicateInDatabase";

    // Export of attendance (FR-10)
    public const string ExportInvalidRange = "Err_Export_InvalidRange";
    public const string ExportWriteFailed = "Err_Export_WriteFailed";

    public const string UnexpectedError = "Err_Unexpected";
}
