using MicroServiceProduct.Infraestructure.DataBase;
using MicroServiceProduct.Infraestructure.Repository;
using MicroServiceProduct.Domain.Services;
using MicroServiceProduct.Domain.Interfaces;
using MicroServiceProduct.Application.Services;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Configure Swagger/Swashbuckle
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MicroServiceProduct API", Version = "v1" });
    // Include XML comments (requires GenerateDocumentationFile=true in csproj)
    var xmlFile = (Assembly.GetExecutingAssembly().GetName().Name ?? "") + ".xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Register controllers so attribute-routed controllers are available
builder.Services.AddControllers();

// Get connection string from configuration (appsettings.json)
var connectionString = builder.Configuration.GetConnectionString("MicroServiceProduct")
                       ?? builder.Configuration["ConnectionStrings:MicroServiceProduct"];
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'MicroServiceProduct' is not configured. Set it in appsettings.json or the CONNECTION_STRING env var.");
}

// Register infrastructure / domain services
// Register IDataBase as a singleton using DataBaseConnection which requires an explicit connection string
builder.Services.AddSingleton<IDataBase>(_ => DataBaseConnection.GetInstance(connectionString));

// Repositories
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();

// Application services
builder.Services.AddScoped<IProductService, ProductService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MicroServiceProduct API v1");
        c.RoutePrefix = string.Empty; // serve Swagger UI at app root (/) in development
    });
}

// Map controller routes (attribute routing)
app.MapControllers();

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
