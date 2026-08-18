using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Api.Authentication;
using TaskManager.Api.ErrorHandling;
using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Security;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Application.Projects.Create;
using TaskManager.Application.Users.Login;
using TaskManager.Application.Users.Register;
using TaskManager.Infrastructure.Persistence;
using TaskManager.Infrastructure.Persistence.Repositories;
using TaskManager.Infrastructure.Security;
using TaskManager.Infrastructure.Time;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------
// API
// -------------------------------------------------------

builder.Services.AddControllers();

builder.Services.AddOpenApi();

// Стандартний формат HTTP API errors.
builder.Services.AddProblemDetails();

// Глобальна обробка Application,
// Domain та unexpected exceptions.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// -------------------------------------------------------
// Database
// -------------------------------------------------------

var connectionString =
    builder.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException(
        "Connection string 'Database' was not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// -------------------------------------------------------
// JWT configuration
// -------------------------------------------------------

var jwtSection =
    builder.Configuration.GetSection(
        JwtOptions.SectionName);

var jwtOptions =
    jwtSection.Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "JWT configuration was not found.");

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(jwtSection)
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

// SigningKey зберігається в User Secrets
// у форматі Base64.
//
// Для перевірки JWT перетворюємо його
// назад у масив bytes.
byte[] jwtSigningKeyBytes;

try
{
    jwtSigningKeyBytes =
        Convert.FromBase64String(
            jwtOptions.SigningKey);
}
catch (FormatException exception)
{
    throw new InvalidOperationException(
        "Jwt:SigningKey must be a valid Base64 string.",
        exception);
}

if (jwtSigningKeyBytes.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:SigningKey must contain at least 32 bytes.");
}

var jwtSecurityKey =
    new SymmetricSecurityKey(
        jwtSigningKeyBytes);

// -------------------------------------------------------
// Authentication
// -------------------------------------------------------

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Не перетворюємо стандартні JWT claim names
        // у Microsoft-specific claim names.
        //
        // sub залишиться sub,
        // email залишиться email.
        options.MapInboundClaims = false;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = jwtSecurityKey,

                ValidateLifetime = true,

                // Допускаємо невелику різницю
                // системного часу.
                ClockSkew = TimeSpan.FromSeconds(30)
            };
    });

// Потрібно для [Authorize].
builder.Services.AddAuthorization();

// -------------------------------------------------------
// Current User
// -------------------------------------------------------

// Дозволяє HttpCurrentUser отримати
// поточний HttpContext.
builder.Services.AddHttpContextAccessor();

// Представлення authenticated користувача
// для Application layer.
builder.Services.AddScoped<
    ICurrentUser,
    HttpCurrentUser>();

// -------------------------------------------------------
// Persistence
// -------------------------------------------------------

// User persistence.
builder.Services.AddScoped<
    IUserRepository,
    UserRepository>();

// Project persistence.
builder.Services.AddScoped<
    IProjectRepository,
    ProjectRepository>();

// AppDbContext реалізує IUnitOfWork.
//
// Repository та UnitOfWork в межах одного
// HTTP request використовують один і той самий
// scoped AppDbContext.
builder.Services.AddScoped<IUnitOfWork>(
    serviceProvider =>
        serviceProvider
            .GetRequiredService<AppDbContext>());

// -------------------------------------------------------
// Security
// -------------------------------------------------------

// Хешування та перевірка password.
builder.Services.AddScoped<
    IPasswordHasher,
    PasswordHasher>();

// Генерація JWT access token.
builder.Services.AddSingleton<
    IAccessTokenGenerator,
    JwtAccessTokenGenerator>();

// -------------------------------------------------------
// Time
// -------------------------------------------------------

builder.Services.AddSingleton<
    IClock,
    SystemClock>();

// -------------------------------------------------------
// Application use cases
// -------------------------------------------------------

// Users.
builder.Services.AddScoped<
    RegisterUserHandler>();

builder.Services.AddScoped<
    LoginUserHandler>();

// Projects.
builder.Services.AddScoped<
    CreateProjectHandler>();

// -------------------------------------------------------
// HTTP pipeline
// -------------------------------------------------------

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Перехоплює необроблені exceptions
// із controller/Application/Domain.
app.UseExceptionHandler();

// Спочатку визначаємо authenticated user.
app.UseAuthentication();

// Потім перевіряємо authorization rules,
// наприклад [Authorize].
app.UseAuthorization();

// Підключаємо controller endpoints.
app.MapControllers();

// Запускаємо ASP.NET Core application.
app.Run();