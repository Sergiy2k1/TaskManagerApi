using TaskManager.Application.Abstractions.Authorization;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Pagination;

namespace TaskManager.Application.Projects.GetMembers;

public sealed class GetProjectMembersHandler : IQueryHandler<GetProjectMembersQuery, PagedResult<GetProjectMembersResult>>
{
    private const int MaxPageSize = 100;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly IProjectAccessPolicy _projectAccessPolicy;

    public GetProjectMembersHandler(IProjectRepository projectRepository, IProjectMemberRepository projectMemberRepository, IProjectAccessPolicy projectAccessPolicy)
    {
        _projectRepository = projectRepository;
        _projectMemberRepository = projectMemberRepository;
        _projectAccessPolicy = projectAccessPolicy;
    }

    public async Task<PagedResult<GetProjectMembersResult>> HandleAsync(GetProjectMembersQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.ProjectId == Guid.Empty) throw new ApplicationValidationException("Project identifier cannot be empty.", nameof(query.ProjectId));
        Validate(query.Page, query.PageSize);

        var project = await _projectRepository.GetByIdAsync(query.ProjectId, cancellationToken);
        if (project is null) throw new ApplicationNotFoundException("Project was not found.");

        await _projectAccessPolicy.EnsureHasAccessAsync(project.OwnerId, project.Id, cancellationToken);

        var page = await _projectMemberRepository.GetActivePageByProjectAsync(project.Id, query.Page, query.PageSize, cancellationToken);
        var items = page.Items.Select(member => new GetProjectMembersResult(
            member.Id,
            member.UserId,
            member.Role,
            member.JoinedAtUtc,
            member.UpdatedAtUtc)).ToArray();

        return new PagedResult<GetProjectMembersResult>(items, page.Page, page.PageSize, page.TotalCount);
    }

    private static void Validate(int page, int pageSize)
    {
        if (page < 1) throw new ApplicationValidationException("Page must be greater than or equal to 1.", nameof(page));
        if (pageSize is < 1 or > MaxPageSize) throw new ApplicationValidationException($"Page size must be between 1 and {MaxPageSize}.", nameof(pageSize));
        if ((long)(page - 1) * pageSize > int.MaxValue) throw new ApplicationValidationException("Requested page is too large.", nameof(page));
    }
}
