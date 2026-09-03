using Microsoft.Extensions.DependencyInjection;
using TaskManager.Application.Abstractions.Authorization;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Common.Authorization;
using TaskManager.Application.Projects.AddMember;
using TaskManager.Application.Projects.ChangeMemberRole;
using TaskManager.Application.Projects.Create;
using TaskManager.Application.Projects.GetById;
using TaskManager.Application.Projects.GetMembers;
using TaskManager.Application.Projects.RemoveMember;
using TaskManager.Application.Tasks.Assign;
using TaskManager.Application.Tasks.Create;
using TaskManager.Application.Tasks.GetById;
using TaskManager.Application.Tasks.GetByProject;
using TaskManager.Application.Tasks.Unassign;
using TaskManager.Application.Tasks.Update;
using TaskManager.Application.Users.Login;
using TaskManager.Application.Users.Register;

namespace TaskManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<
            IProjectMemberManagementPolicy,
            ProjectMemberManagementPolicy>();

        services.AddScoped<
            IProjectAccessPolicy,
            ProjectAccessPolicy>();

        services.AddScoped<
            ICommandHandler<RegisterUserCommand, RegisterUserResult>,
            RegisterUserHandler>();

        services.AddScoped<
            ICommandHandler<LoginUserCommand, LoginUserResult>,
            LoginUserHandler>();

        services.AddScoped<
            ICommandHandler<CreateProjectCommand, CreateProjectResult>,
            CreateProjectHandler>();

        services.AddScoped<
            ICommandHandler<AddProjectMemberCommand, AddProjectMemberResult>,
            AddProjectMemberHandler>();

        services.AddScoped<
            IQueryHandler<GetProjectByIdQuery, GetProjectByIdResult>,
            GetProjectByIdHandler>();

        services.AddScoped<
            ICommandHandler<
                ChangeProjectMemberRoleCommand,
                ChangeProjectMemberRoleResult>,
            ChangeProjectMemberRoleHandler>();

        services.AddScoped<
            ICommandHandler<
                RemoveProjectMemberCommand,
                RemoveProjectMemberResult>,
            RemoveProjectMemberHandler>();

        services.AddScoped<
            IQueryHandler<
                GetProjectMembersQuery,
                IReadOnlyList<GetProjectMembersResult>>,
            GetProjectMembersHandler>();

        services.AddScoped<
            ICommandHandler<CreateTaskCommand, CreateTaskResult>,
            CreateTaskHandler>();

        services.AddScoped<
            IQueryHandler<GetTaskByIdQuery, GetTaskByIdResult>,
            GetTaskByIdHandler>();

        services.AddScoped<
            IQueryHandler<
                GetProjectTasksQuery,
                IReadOnlyList<GetProjectTasksResult>>,
            GetProjectTasksHandler>();

        services.AddScoped<
            ICommandHandler<UpdateTaskCommand, UpdateTaskResult>,
            UpdateTaskHandler>();

        services.AddScoped<
            ICommandHandler<AssignTaskCommand, AssignTaskResult>,
            AssignTaskHandler>();

        services.AddScoped<
            ICommandHandler<UnassignTaskCommand, UnassignTaskResult>,
            UnassignTaskHandler>();

        return services;
    }
}