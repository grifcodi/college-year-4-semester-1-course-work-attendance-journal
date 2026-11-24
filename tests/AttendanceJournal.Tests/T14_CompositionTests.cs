using AttendanceJournal.Services.Interfaces;
using AttendanceJournal.ViewModels;
using AttendanceJournal.ViewModels.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AttendanceJournal.Tests;
public class T14_CompositionTests
{
    private static ServiceProvider BuildProvider() =>
        new ServiceCollection()
            .AddAttendanceJournal("Data Source=:memory:")
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

    [Fact]
    public void Container_BuildsWithValidation()
    {
        using var provider = BuildProvider();

        Assert.NotNull(provider);
    }

    [Theory]
    [InlineData(typeof(ISubjectService))]
    [InlineData(typeof(IStudentGroupService))]
    [InlineData(typeof(IStudentService))]
    [InlineData(typeof(IErrorLog))]
    [InlineData(typeof(IConfirmationService))]
    [InlineData(typeof(GroupListViewModel))]
    [InlineData(typeof(StudentListViewModel))]
    [InlineData(typeof(GroupsPageViewModel))]
    public void Registered_Types_CanBeResolved(Type type)
    {
        using var provider = BuildProvider();

        Assert.NotNull(provider.GetRequiredService(type));
    }

    [Fact]
    public void ViewModels_AreTransient_Services_AreSingletons()
    {
        using var provider = BuildProvider();

        Assert.NotSame(
            provider.GetRequiredService<GroupsPageViewModel>(),
            provider.GetRequiredService<GroupsPageViewModel>());
        Assert.Same(
            provider.GetRequiredService<IStudentService>(),
            provider.GetRequiredService<IStudentService>());
    }
}