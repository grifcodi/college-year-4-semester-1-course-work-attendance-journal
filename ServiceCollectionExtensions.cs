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
        services.AddSingleton<IMonthGridService, MonthGridService>();
        services.AddSingleton<IAuditLogService, AuditLogService>();
        services.AddSingleton<IStudentFileReader, StudentFileReader>();
        services.AddSingleton<IStudentImportService, StudentImportService>();
        services.AddSingleton<IAttendanceExportService, AttendanceExportService>();

        // Avalonia implementations of the interfaces that view models use (Views/).
        services.AddSingleton<IThemeApplier, AvaloniaThemeApplier>();
        services.AddSingleton<IConfirmationService, DialogConfirmationService>();
        services.AddSingleton<IFileDialogService, AvaloniaFileDialogService>();

        services.AddTransient<SettingsPageViewModel>();
        services.AddTransient<GroupListViewModel>();
        services.AddTransient<StudentListViewModel>();
        services.AddTransient<StudentImportViewModel>();
        services.AddTransient<GroupsPageViewModel>();
        services.AddTransient<SubjectsPageViewModel>();
        services.AddTransient<JournalPageViewModel>();
        services.AddTransient<MonthGridPageViewModel>();
        services.AddTransient<SummaryPageViewModel>();
        services.AddTransient<AuditLogPageViewModel>();
        services.AddTransient<MainViewModel>();

        return services;
    }
}
