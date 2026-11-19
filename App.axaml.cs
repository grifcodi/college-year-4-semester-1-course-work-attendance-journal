using AttendanceJournal.Data;
using AttendanceJournal.Services.Common;
using AttendanceJournal.ViewModels;
using AttendanceJournal.ViewModels.Localization;
using AttendanceJournal.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AttendanceJournal;

public partial class App : Application
{
    public IServiceProvider? ServiceProvider { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        RegisterGlobalExceptionLogging();

        ServiceProvider = new ServiceCollection()
            .AddAttendanceJournal()
            .BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = ServiceProvider.GetRequiredService<GroupsPageViewModel>();
            desktop.MainWindow = new MainWindow { DataContext = vm };
            desktop.Exit += (_, _) => (ServiceProvider as IDisposable)?.Dispose();

            _ = StartAsync(vm);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task StartAsync(GroupsPageViewModel vm)
    {
        try
        {
            var dbFactory = ServiceProvider!.GetRequiredService<IDbContextFactory<AppDbContext>>();
            await DatabaseInitializer.InitializeAsync(dbFactory);
            await vm.InitializeAsync();
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("Startup", ex);

            var message = Localizer.TranslateError(Result.Failure(ErrorKeys.DatabaseUnavailable));
            vm.SetStartupFailure(message, () => StartAsync(vm));
        }
    }

    private static void RegisterGlobalExceptionLogging()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            CrashLogger.LogException(
                "UnhandledException",
                e.ExceptionObject as Exception ?? new InvalidOperationException("Unknown unhandled exception"));

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            CrashLogger.LogException("UnobservedTaskException", e.Exception);
            e.SetObserved();
        };
    }
}