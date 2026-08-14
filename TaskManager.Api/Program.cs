using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

// SigningKey у нас зберігається як Base64,
// тому для JWT validation перетворюємо його назад у bytes.
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
        // Не перетворюємо JWT claim names
        // у Microsoft-specific claim names.
        // "sub" залишиться "sub",
        // "email" залишиться "email".
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

                // Невеликий tolerance для різниці часу
                // між системами.
                ClockSkew = TimeSpan.FromSeconds(30)
            };
    });

// [Authorize] використовує authorization services.
builder.Services.AddAuthorization();

// -------------------------------------------------------
// Persistence
// -------------------------------------------------------

builder.Services.AddScoped<
    IUserRepository,
    UserRepository>();

builder.Services.AddScoped<IUnitOfWork>(
    serviceProvider =>
        serviceProvider
            .GetRequiredService<AppDbContext>());

// -------------------------------------------------------
// Security services
// -------------------------------------------------------

builder.Services.AddScoped<
    IPasswordHasher,
    PasswordHasher>();

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

builder.Services.AddScoped<
    RegisterUserHandler>();

builder.Services.AddScoped<
    LoginUserHandler>();

// -------------------------------------------------------
// HTTP pipeline
// -------------------------------------------------------

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Спочатку визначаємо, хто користувач.
app.UseAuthentication();

// Потім перевіряємо, чи має він право
// виконувати конкретну операцію.
app.UseAuthorization();

app.MapControllers();

app.Run();