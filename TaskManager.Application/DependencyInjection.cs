using Microsoft.Extensions.DependencyInjection;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Projects.AddMember;
using TaskManager.Application.Projects.Create;
using TaskManager.Application.Projects.GetById;
using TaskManager.Application.Users.Login;
using TaskManager.Application.Users.Register;
using TaskManager.Application.Projects.ChangeMemberRole;
using TaskManager.Application.Projects.RemoveMember;

namespace TaskManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
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
        return services;
    }
}