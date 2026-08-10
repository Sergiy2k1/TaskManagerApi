using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Abstractions.Time;
using TaskManager.Infrastructure.Persistence;
using TaskManager.Infrastructure.Time;

var builder = WebApplication.CreateBuilder(args);

// Controller-based API.
builder.Services.AddControllers();

// OpenAPI.
builder.Services.AddOpenApi();

// Отримуємо connection string із конфігурації.
var connectionString =
    builder.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException(
        "Connection string 'Database' was not found.");

// Реєструємо EF Core DbContext та PostgreSQL provider.
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// AppDbContext реалізує IUnitOfWork.
// В межах одного HTTP request ми отримаємо той самий DbContext.
builder.Services.AddScoped<IUnitOfWork>(serviceProvider =>
    serviceProvider.GetRequiredService<AppDbContext>());

// SystemClock не має стану, тому одного instance достатньо
// для всього застосунку.
builder.Services.AddSingleton<IClock, SystemClock>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();