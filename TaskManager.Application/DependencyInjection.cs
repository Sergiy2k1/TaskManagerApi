using Microsoft.Extensions.DependencyInjection;
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

        return services;
    }
}
