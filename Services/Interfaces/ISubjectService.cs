


using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AttendanceJournal.Services.Interfaces;

public interface ISubjectService
{
    Task<List<Subject>> GetAllAsync(bool includeArchived = false);
    Task<Result<Subject>> CreateAsync(string name);
    Task<Result<Subject>> UpdateAsync(int subjectId, string newName);
    Task<Result> ArchiveAsync(int subjectId);
    Task<Result> UnarchiveAsync(int subjectId);
}
