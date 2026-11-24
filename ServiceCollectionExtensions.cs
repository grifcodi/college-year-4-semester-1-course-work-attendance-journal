using AttendanceJournal.Data;
using AttendanceJournal.Services;
using AttendanceJournal.Services.Common;
using AttendanceJournal.Services.Interfaces;
using AttendanceJournal.ViewModels;
using AttendanceJournal.ViewModels.Interfaces;
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
        services.AddSingleton<ILessonService, LessonService>();
        services.AddSingleton<IAttendanceService, AttendanceService>();
        services.AddSingleton<IErrorLog, FileErrorLog>();
        services.AddSingleton<ISummaryService, SummaryService>();
        services.AddSingleton<ISettingService, SettingService>();
        services.AddSingleton<IThemeApplier, AvaloniaThemeApplier>();
        services.AddSingleton<IConfirmationService, DialogConfirmationService>();

        services.AddTransient<SettingsPageViewModel>();
        services.AddTransient<GroupListViewModel>();
        services.AddTransient<StudentListViewModel>();
        services.AddTransient<GroupsPageViewModel>();
        services.AddTransient<MainViewModel>();

        return services;
    }
}