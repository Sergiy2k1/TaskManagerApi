using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Security;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Users.Login;
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

// Налаштування JWT.
// Частина значень береться з appsettings.json,
// SigningKey — з User Secrets.
builder.Services
    .AddOptions<JwtOptions>()
    .Bind(
        builder.Configuration.GetSection(
            JwtOptions.SectionName))
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(options.Issuer),
        "Jwt:Issuer is required.")
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(options.Audience),
        "Jwt:Audience is required.")
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(options.SigningKey),
        "Jwt:SigningKey is required.")
    .Validate(
        options =>
            options.AccessTokenLifetimeMinutes
                is >= 1 and <= 60,
        "Jwt:AccessTokenLifetimeMinutes must be between 1 and 60.")
    .ValidateOnStart();

// Repository для роботи з користувачами.
builder.Services.AddScoped<IUserRepository, UserRepository>();

// AppDbContext реалізує IUnitOfWork.
// Repository та UnitOfWork використовують один scoped AppDbContext.
builder.Services.AddScoped<IUnitOfWork>(serviceProvider =>
    serviceProvider.GetRequiredService<AppDbContext>());

// Хешування та перевірка паролів.
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

// Генератор JWT access token.
builder.Services.AddSingleton<
    IAccessTokenGenerator,
    JwtAccessTokenGenerator>();

// Системний UTC clock.
builder.Services.AddSingleton<IClock, SystemClock>();

// Application use cases.
builder.Services.AddScoped<RegisterUserHandler>();
builder.Services.AddScoped<LoginUserHandler>();

var app = builder.Build();

// OpenAPI вмикаємо тільки в Development.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// HTTP-запити перенаправляємо на HTTPS,
// якщо HTTPS endpoint доступний.
app.UseHttpsRedirection();

// Підключаємо маршрути controller-ів.
app.MapControllers();

// Запускаємо ASP.NET Core application.
app.Run();