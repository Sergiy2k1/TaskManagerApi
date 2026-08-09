var builder = WebApplication.CreateBuilder(args);

// Реєструємо підтримку controller-based API.
builder.Services.AddControllers();

// Генерація OpenAPI-документа з описом наших endpoint-ів.
builder.Services.AddOpenApi();

var app = builder.Build();

// OpenAPI вмикаємо тільки в Development-середовищі.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// HTTP-запити будуть перенаправлятися на HTTPS.
app.UseHttpsRedirection();

// Знаходимо всі контролери та підключаємо їх маршрути.
app.MapControllers();

// Запускаємо вебзастосунок.
app.Run();