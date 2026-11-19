using AttendanceJournal.Data;
using AttendanceJournal.Services;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Services.Interfaces;
using AttendanceJournal.ViewModels;
using AttendanceJournal.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AttendanceJournal;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAttendanceJournal(
        this IServiceCollection services,
        string? connectionString = null)
    {
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite(connectionString ?? DatabaseInitializer.GetConnectionString()));

        services.AddSingleton<ISubjectService, SubjectService>();
        services.AddSingleton<IStudentGroupService, StudentGroupService>();
        services.AddSingleton<IStudentService, StudentService>();
        services.AddSingleton<IErrorLog, FileErrorLog>();

        services.AddSingleton<IConfirmationService, DialogConfirmationService>();

        services.AddTransient<GroupListViewModel>();
        services.AddTransient<StudentListViewModel>();
        services.AddTransient<GroupsPageViewModel>();

        return services;
    }
}