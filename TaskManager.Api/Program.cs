using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Security;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Users.Register;
using TaskManager.Infrastructure.Persistence;
using TaskManager.Infrastructure.Persistence.Repositories;
using TaskManager.Infrastructure.Security;
using TaskManager.Infrastructure.Time;

var builder = WebApplication.CreateBuilder(args);

// Controller-based API.
builder.Services.AddControllers();

// OpenAPI.
builder.Services.AddOpenApi();

// Отримуємо connection string із конфігурації.
// Для локальної розробки він зберігається через User Secrets.
var connectionString =
    builder.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException(
        "Connection string 'Database' was not found.");

// Реєструємо EF Core DbContext та PostgreSQL provider.
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// Repository для роботи з користувачами.
builder.Services.AddScoped<IUserRepository, UserRepository>();

// AppDbContext реалізує IUnitOfWork.
// Repository та UnitOfWork працюють з одним scoped DbContext.
builder.Services.AddScoped<IUnitOfWork>(serviceProvider =>
    serviceProvider.GetRequiredService<AppDbContext>());

// Сервіс хешування та перевірки паролів.
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

// SystemClock не зберігає mutable state,
// тому одного instance достатньо на весь application lifetime.
builder.Services.AddSingleton<IClock, SystemClock>();

// Application use case для реєстрації користувача.
builder.Services.AddScoped<RegisterUserHandler>();

var app = builder.Build();

// OpenAPI вмикаємо тільки в Development.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// HTTP-запити перенаправляємо на HTTPS,
// якщо HTTPS endpoint доступний.
app.UseHttpsRedirection();

// Підключаємо маршрути всіх controller-ів.
app.MapControllers();

// Запускаємо ASP.NET Core application.
app.Run();