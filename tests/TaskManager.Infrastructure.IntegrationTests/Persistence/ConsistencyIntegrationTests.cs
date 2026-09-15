using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Infrastructure.IntegrationTests.Database;
using TaskManager.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TaskManager.Infrastructure.IntegrationTests.Persistence;

[Collection(PostgreSqlCollectionDefinition.Name)]
public sealed class ConsistencyIntegrationTests
{
    private readonly PostgreSqlFixture _fixture;

    public ConsistencyIntegrationTests(
        PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DuplicateEmailAfterConcurrentPreChecksReturnsConflict()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        var email =
            $"registration-race-{Guid.NewGuid():N}@example.com";

        var firstUser =
            User.Create(
                email,
                "First User",
                "integration-password-hash",
                DateTimeOffset.UtcNow);

        var secondUser =
            User.Create(
                email.ToUpperInvariant(),
                "Second User",
                "integration-password-hash",
                DateTimeOffset.UtcNow);

        await using var firstDb =
            _fixture.CreateDbContext();

        await using var secondDb =
            _fixture.CreateDbContext();

        var firstRepository =
            new UserRepository(firstDb);

        var secondRepository =
            new UserRepository(secondDb);

        Assert.False(
            await firstRepository
                .ExistsByNormalizedEmailAsync(
                    firstUser.NormalizedEmail,
                    cancellationToken));

        Assert.False(
            await secondRepository
                .ExistsByNormalizedEmailAsync(
                    secondUser.NormalizedEmail,
                    cancellationToken));

        firstRepository.Add(firstUser);
        secondRepository.Add(secondUser);

        await firstDb.SaveChangesAsync(
            cancellationToken);

        var exception =
            await Assert.ThrowsAsync<ApplicationConflictException>(
                () => secondDb.SaveChangesAsync(
                    cancellationToken));

        Assert.Equal(
            "A user with this email already exists.",
            exception.Message);

        Assert.IsType<DbUpdateException>(
            exception.InnerException);
    }

    [Fact]
    public async Task FailedMultiWriteSaveChangesRollsBackWholeBatch()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        var now =
            DateTimeOffset.UtcNow;

        var owner =
            User.Create(
                $"atomic-owner-{Guid.NewGuid():N}@example.com",
                "Atomic Owner",
                "integration-password-hash",
                now);

        await using (var setupDb =
                     _fixture.CreateDbContext())
        {
            setupDb.Users.Add(owner);

            await setupDb.SaveChangesAsync(
                cancellationToken);
        }

        var project =
            Project.Create(
                owner.Id,
                "Atomic Project",
                null,
                now);

        var firstMembership =
            ProjectMember.Create(
                project.Id,
                owner.Id,
                ProjectMemberRole.Manager,
                now);

        var duplicateMembership =
            ProjectMember.Create(
                project.Id,
                owner.Id,
                ProjectMemberRole.Manager,
                now);

        await using (var failingDb =
                     _fixture.CreateDbContext())
        {
            failingDb.Projects.Add(project);

            failingDb.ProjectMembers.AddRange(
                firstMembership,
                duplicateMembership);

            var exception =
                await Assert.ThrowsAsync<ApplicationConflictException>(
                    () => failingDb.SaveChangesAsync(
                        cancellationToken));

            Assert.Equal(
                "User is already an active project member.",
                exception.Message);
        }

        await using var verificationDb =
            _fixture.CreateDbContext();

        var projectPersisted =
            await verificationDb.Projects
                .AsNoTracking()
                .AnyAsync(
                    item => item.Id == project.Id,
                    cancellationToken);

        var membershipsPersisted =
            await verificationDb.ProjectMembers
                .AsNoTracking()
                .CountAsync(
                    member =>
                        member.ProjectId == project.Id,
                    cancellationToken);

        Assert.False(projectPersisted);
        Assert.Equal(0, membershipsPersisted);
    }
}
