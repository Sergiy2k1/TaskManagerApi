using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Pagination;

namespace TaskManager.Application.Projects.GetAll;

public sealed class GetProjectsHandler : IQueryHandler<GetProjectsQuery, PagedResult<GetProjectsResult>>
{
    private const int MaxPageSize = 100;
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUser _currentUser;

    public GetProjectsHandler(IProjectRepository projectRepository, ICurrentUser currentUser)
    {
        _projectRepository = projectRepository;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<GetProjectsResult>> HandleAsync(GetProjectsQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        Validate(query.Page, query.PageSize);

        var page = await _projectRepository.GetAccessiblePageByUserIdAsync(
            _currentUser.UserId,
            query.Page,
            query.PageSize,
            cancellationToken);

        var items = page.Items.Select(project => new GetProjectsResult(
            project.Id,
            project.OwnerId,
            project.Name,
            project.Description,
            project.IsArchived,
            project.CreatedAtUtc,
            project.UpdatedAtUtc,
            project.ArchivedAtUtc)).ToArray();

        return new PagedResult<GetProjectsResult>(items, page.Page, page.PageSize, page.TotalCount);
    }

    private static void Validate(int page, int pageSize)
    {
        if (page < 1) throw new ApplicationValidationException("Page must be greater than or equal to 1.", nameof(page));
        if (pageSize is < 1 or > MaxPageSize) throw new ApplicationValidationException($"Page size must be between 1 and {MaxPageSize}.", nameof(pageSize));
        if ((long)(page - 1) * pageSize > int.MaxValue) throw new ApplicationValidationException("Requested page is too large.", nameof(page));
    }
}
