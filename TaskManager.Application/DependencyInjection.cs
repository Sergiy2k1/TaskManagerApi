using Microsoft.Extensions.DependencyInjection;
using TaskManager.Application.Abstractions.Messaging;
using TaskManager.Application.Projects.Create;
using TaskManager.Application.Projects.GetById;
using TaskManager.Application.Users.Login;
using TaskManager.Application.Users.Register;

namespace TaskManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<LoginUserHandler>();
        services.AddScoped<CreateProjectHandler>();
        services.AddScoped<GetProjectByIdHandler>();

        services.AddScoped<
            ICommandHandler<RegisterUserCommand, RegisterUserResult>>(
            serviceProvider =>
                serviceProvider.GetRequiredService<RegisterUserHandler>());

        services.AddScoped<
            ICommandHandler<LoginUserCommand, LoginUserResult>>(
            serviceProvider =>
                serviceProvider.GetRequiredService<LoginUserHandler>());

        services.AddScoped<
            ICommandHandler<CreateProjectCommand, CreateProjectResult>>(
            serviceProvider =>
                serviceProvider.GetRequiredService<CreateProjectHandler>());

        services.AddScoped<
            IQueryHandler<GetProjectByIdQuery, GetProjectByIdResult>>(
            serviceProvider =>
                serviceProvider.GetRequiredService<GetProjectByIdHandler>());

        return services;
    }
}