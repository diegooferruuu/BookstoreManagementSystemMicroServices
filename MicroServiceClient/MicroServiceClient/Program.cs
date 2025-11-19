using MicroServiceClient.Domain.Interfaces;
using MicroServiceClient.Application.Services;
using MicroServiceClient.Infrastructure.DataBase;
using MicroServiceClient.Infrastructure.Repositories;


var builder = WebApplication.CreateBuilder(args);


// Conexión (lee de appsettings.json -> ConnectionStrings:Default)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection no configurada.");

// Registro de dependencias
builder.Services.AddSingleton<IDataBase>(sp => DataBaseConnection.GetInstance(connectionString));
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IClientService, ClientService>();


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

app.UseAuthorization();

app.MapControllers();

app.Run();
