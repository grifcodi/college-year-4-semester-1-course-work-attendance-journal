using AttendanceJournal.Models;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Services.Interfaces;
using AttendanceJournal.Tests.TestHelpers;
using AttendanceJournal.ViewModels;

namespace AttendanceJournal.Tests;

[Collection(AttendanceJournal.Tests.TestHelpers.LocalizerCollection.Name)]
public class T17b_StaleLoadTests
{
    private sealed class GatedStudentService : IStudentService
    {
        public Dictionary<int, TaskCompletionSource<List<Student>>> Gates { get; } = [];

        public Task<List<Student>> GetByGroupAsync(int groupId, bool includeArchived = false) =>
            Gates[groupId].Task;

        public Task<Result<Student>> AddAsync(int groupId, string fullName) => throw new NotSupportedException();
        public Task<Result<Student>> UpdateAsync(int studentId, string newFullName) => throw new NotSupportedException();
        public Task<Result> ArchiveAsync(int studentId) => throw new NotSupportedException();
        public Task<Result> UnarchiveAsync(int studentId) => throw new NotSupportedException();
    }

    private static Student MakeStudent(int id, int groupId, string name) =>
        new() { StudentId = id, StudentGroupId = groupId, FullName = name };

    [Fact]
    public async Task SlowLoadForOldGroup_DoesNotOverwriteNewerGroupList()
    {
        var service = new GatedStudentService();
        service.Gates[1] = new(TaskCreationOptions.RunContinuationsAsynchronously);
        service.Gates[2] = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var vm = new StudentListViewModel(service, new FakeConfirmationService(), new FakeErrorLog());

        var first = vm.LoadForGroupAsync(1);
        var second = vm.LoadForGroupAsync(2);
        service.Gates[2].SetResult([MakeStudent(2, 2, "Б")]);
        await second;
        service.Gates[1].SetResult([MakeStudent(1, 1, "А")]);
        await first;

        Assert.Equal(["Б"], vm.Students.Select(s => s.FullName));
    }
}


