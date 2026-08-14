using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Api.ErrorHandling;
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

// -------------------------------------------------------
// API
// -------------------------------------------------------

builder.Services.AddControllers();

// OpenAPI documentation.
builder.Services.AddOpenApi();

// Стандартний формат помилок HTTP API.
builder.Services.AddProblemDetails();

// Глобальна обробка Application, Domain
// та unexpected exceptions.
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

// SigningKey зберігається у User Secrets як Base64.
// Для JWT validation перетворюємо його назад у bytes.
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
        // Залишаємо стандартні JWT claim names:
        // sub, email, jti тощо.
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

                // Допустима невелика різниця системного часу.
                ClockSkew = TimeSpan.FromSeconds(30)
            };
    });

// Потрібно для [Authorize].
builder.Services.AddAuthorization();

// -------------------------------------------------------
// Persistence
// -------------------------------------------------------

builder.Services.AddScoped<
    IUserRepository,
    UserRepository>();

// AppDbContext реалізує IUnitOfWork.
// У межах одного HTTP request repository та unit of work
// отримують той самий scoped AppDbContext.
builder.Services.AddScoped<IUnitOfWork>(
    serviceProvider =>
        serviceProvider
            .GetRequiredService<AppDbContext>());

// -------------------------------------------------------
// Security
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

// Перехоплює необроблені exceptions нижче по pipeline.
app.UseExceptionHandler();

// Спочатку ASP.NET Core визначає,
// хто виконує request.
app.UseAuthentication();

// Потім перевіряє authorization rules,
// зокрема [Authorize].
app.UseAuthorization();

// Підключає controller endpoints.
app.MapControllers();

app.Run();