using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AttendanceJournal.Services.Interfaces;

public interface IStudentGroupService
{
    Task<List<StudentGroup>> GetGroupAsync(bool includeArchived = false);
    Task<Result<StudentGroup>> CreateGroupAsync(string name, string academicYear);
    Task<Result<StudentGroup>> UpdateGroupAsync(int groupId, string newName, string newAcademicYear);
    Task<Result> ArchiveGroupAsync(int groupId);
    Task<Result> UnarchiveGroupAsync(int groupId);

    Task<List<Student>> GetStudentsByGroupAsync(int groupId, bool includeArchived = false);
    Task<Result<Student>> AddStudentAsync(int groupId, string fullName);
    Task<Result<Student>> UpdateStudentAsync(int studentId, string newFullName);
    Task<Result> ArchiveStudentAsync(int studentId);
    Task<Result> UnarchiveStudentAsync(int studentId);
}
