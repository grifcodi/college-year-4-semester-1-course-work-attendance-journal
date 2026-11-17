using System;
using System.Collections.Generic;
using System.Text;

namespace AttendanceJournal.Services.Common;

public static class ErrorKeys
{
    public const string DatabaseConflict = "Err_DatabaseConflict";
    public const string EntityNotFound = "Err_EntityNotFound";

    public const string SubjectNameRequired = "Err_Subject_NameRequired";
    public const string SubjectNameTooLong = "Err_Subject_NameTooLong";
    public const string SubjectAlreadyExists = "Err_Subject_AlreadyExists";

    public const string GroupNameRequired = "Err_Group_NameRequired";
    public const string GroupNameTooLong = "Err_Group_NameTooLong";
    public const string GroupYearReqired = "Err_Group_YearRequired";
    public const string GroupYearTooLong = "Err_Group_YearTooLong";
    public const string GroupAlreadyExists = "Err_Group_AlreadyExists";
    public const string GroupUnarchiveConflict = "Err_Group_UnarchiveConflict";

    public const string StudentNameRequired = "Err_Student_NameRequired";
    public const string StudentNameTooLong = "Err_Student_NameTooLong";
    public const string StudentAlreadyExists = "Err_Student_AlreadyExists";
}
