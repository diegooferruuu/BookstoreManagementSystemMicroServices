using MicroServiceUsers.Domain.Interfaces;
using MicroServiceUsers.Application.Services;
using MicroServiceUsers.Application.Facade;
using MicroServiceUsers.Infrastructure.DataBase;
using MicroServiceUsers.Infrastructure.Repositories;
using MicroServiceUsers.Infrastructure.Auth;
using MicroServiceUsers.Infrastructure.Security;
using MicroServiceUsers.Infrastructure.Email;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Conexión (lee de appsettings.json -> ConnectionStrings:Default)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection no configurada.");

// Configuración JWT
var jwtOptions = new JwtOptions();
builder.Configuration.GetSection("Jwt").Bind(jwtOptions);
builder.Services.AddSingleton(jwtOptions);

// Configuración SendGrid
var sendGridOptions = new SendGridOptions();
builder.Configuration.GetSection("SendGrid").Bind(sendGridOptions);
builder.Services.AddSingleton(sendGridOptions);

// Autenticación JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key))
    };
});

// Registro de dependencias
builder.Services.AddSingleton<IDataBase>(sp => DataBaseConnection.GetInstance(connectionString));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserService, UserService>();

// Servicios de autenticación y generación
builder.Services.AddScoped<ITokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IJwtAuthService, JwtAuthService>();
builder.Services.AddScoped<IPasswordGenerator, SecurePasswordGenerator>();
builder.Services.AddScoped<IUsernameGenerator, UsernameGenerator>();
builder.Services.AddScoped<IEmailService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<SendGridEmailService>>();
    var options = sp.GetRequiredService<SendGridOptions>();
    return new SendGridEmailService(options, logger);
});

// Fachada
builder.Services.AddScoped<IUserFacade, UserFacade>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Seed de la base de datos
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userRepository = services.GetRequiredService<IUserRepository>();
    var logger = services.GetRequiredService<ILogger<DatabaseSeeder>>();
    
    var seeder = new DatabaseSeeder(userRepository, logger);
    await seeder.SeedAsync();
}

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
