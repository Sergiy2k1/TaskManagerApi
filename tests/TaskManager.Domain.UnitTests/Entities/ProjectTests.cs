using TaskManager.Domain.Entities;
using TaskManager.Domain.Exceptions;

namespace TaskManager.Domain.UnitTests.Entities;

public sealed class ProjectTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 8, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithValidData_CreatesProject()
    {
        var ownerId = Guid.NewGuid();

        var project = Project.Create(
            ownerId,
            "  Senior Task Manager  ",
            "  Production-grade pet project  ",
            CreatedAtUtc);

        Assert.NotEqual(Guid.Empty, project.Id);
        Assert.Equal(ownerId, project.OwnerId);
        Assert.Equal("Senior Task Manager", project.Name);
        Assert.Equal("Production-grade pet project", project.Description);
        Assert.False(project.IsArchived);
        Assert.Equal(CreatedAtUtc, project.CreatedAtUtc);
        Assert.Null(project.UpdatedAtUtc);
        Assert.Null(project.ArchivedAtUtc);
    }

    [Fact]
    public void Archive_WhenProjectIsActive_ArchivesProject()
    {
        var project = CreateProject();
        var archivedAtUtc = CreatedAtUtc.AddHours(1);

        project.Archive(archivedAtUtc);

        Assert.True(project.IsArchived);
        Assert.Equal(archivedAtUtc, project.ArchivedAtUtc);
        Assert.Equal(archivedAtUtc, project.UpdatedAtUtc);
    }

    [Fact]
    public void Rename_WhenProjectIsArchived_ThrowsDomainConflictException()
    {
        var project = CreateProject();
        project.Archive(CreatedAtUtc.AddHours(1));

        var exception = Assert.Throws<DomainConflictException>(() =>
            project.Rename(
                "Renamed project",
                CreatedAtUtc.AddHours(2)));

        Assert.Equal(
            "Archived project cannot be modified.",
            exception.Message);
    }

    private static Project CreateProject()
    {
        return Project.Create(
            Guid.NewGuid(),
            "Task Manager",
            null,
            CreatedAtUtc);
    }
}
