using Microsoft.EntityFrameworkCore;
using MicroServiceReports.Infraestructure.Persistence;
using MicroServiceReports.Infraestructure.Rabbit;
using MicroServiceReports.Domain.Ports;
using MicroServiceReports.Application.UseCases;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var configuration = builder.Configuration;

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register RabbitMQ settings
builder.Services.Configure<RabbitSettings>(configuration.GetSection("RabbitMq"));

// Register DbContext - PostgreSQL
var connectionString = configuration.GetConnectionString("MicroServiceReports");
builder.Services.AddDbContext<MicroServiceReportsDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register repository
builder.Services.AddScoped<ISaleEventRepository, SaleEventRepositoryEf>();

// Register application handlers
builder.Services.AddScoped<GetSaleBySaleIdHandler>();

// Register background service to consume RabbitMQ
builder.Services.AddHostedService<SalesConfirmedBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Endpoint para obtener todos los reportes
app.MapGet("/api/reports", async (ISaleEventRepository repo) =>
{
    var records = await repo.GetAllAsync();
    return Results.Ok(records.Select(r => new
    {
        id = r.Id,
        saleId = r.SaleId,
        payload = System.Text.Json.JsonDocument.Parse(r.Payload),
        exchange = r.Exchange,
        routingKey = r.RoutingKey,
        receivedAt = r.ReceivedAt
    }));
})
.WithName("GetAllReports")
.WithOpenApi();

// Endpoint para obtener reporte por SaleId
app.MapGet("/api/reports/sale/{saleId:long}", async (long saleId, GetSaleBySaleIdHandler handler) =>
{
    var record = await handler.HandleAsync(saleId);
    if (record == null)
    {
        return Results.NotFound(new { message = $"No se encontró reporte para la venta {saleId}" });
    }

    return Results.Ok(new
    {
        id = record.Id,
        saleId = record.SaleId,
        payload = System.Text.Json.JsonDocument.Parse(record.Payload),
        exchange = record.Exchange,
        routingKey = record.RoutingKey,
        receivedAt = record.ReceivedAt
    });
})
.WithName("GetSaleBySaleId")
.WithOpenApi();

// Endpoint de salud
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "MicroServiceReports" }))
.WithName("HealthCheck")
.WithOpenApi();

app.Run();
