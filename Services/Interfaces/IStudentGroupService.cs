using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AttendanceJournal.Services.Interfaces;

public interface IStudentGroupService
{
    Task<List<StudentGroup>> GetAllAsync(bool includeArchived = false);
    Task<Result<StudentGroup>> CreateAsync(string name, string academicYear);
    Task<Result<StudentGroup>> UpdateAsync(int groupId, string newName, string newAcademicYear);
    Task<Result> ArchiveAsync(int groupId);
    Task<Result> UnarchiveAsync(int groupId);
}
