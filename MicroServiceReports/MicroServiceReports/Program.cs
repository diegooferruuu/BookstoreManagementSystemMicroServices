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

app.MapGet("/api/sales/{saleId:long}", async (long saleId, GetSaleBySaleIdHandler handler) =>
{
    var record = await handler.HandleAsync(saleId);
    if (record == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new
    {
        id = record.Id,
        saleId = record.SaleId,
        payload = record.Payload,
        exchange = record.Exchange,
        routingKey = record.RoutingKey,
        receivedAt = record.ReceivedAt
    });
})
.WithName("GetSaleBySaleId")
.WithOpenApi();

app.Run();
