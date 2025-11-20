using MicroServiceDistributors.Application.Services;
using MicroServiceDistributors.Domain.Interfaces;
using MicroServiceDistributors.Infraestructure.DataBase;
using MicroServiceUsers.Domain.Interfaces;
using MicroservicioCliente.Infrastucture.Persistence;
using MySql.Data.MySqlClient;
using ServiceDistributors.Infrastructure.Repositories;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Conexión (lee de appsettings.json -> ConnectionStrings:Default)
var connectionString = builder.Configuration.GetConnectionString("MysqlMicroServiceDistributorsoDB")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection no configurada.");


// Registro de dependencias
builder.Services.AddSingleton<IDataBase>(sp => DataBaseConnection.GetInstance(connectionString));
// Registro de dependencias personalizadas
builder.Services.AddScoped<IDistributorRepository, DistributorRepository>();
builder.Services.AddScoped<IDistributorService, DistributorService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
