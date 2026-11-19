using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;

namespace AttendanceJournal.Services.Interfaces;

public interface IStudentService
{
    Task<List<Student>> GetByGroupAsync(int groupId, bool includeArchived = false);
    Task<Result<Student>> AddAsync(int groupId, string fullName);
    Task<Result<Student>> UpdateAsync(int studentId, string newFullName);
    Task<Result> ArchiveAsync(int studentId);
    Task<Result> UnarchiveAsync(int studentId);
}
