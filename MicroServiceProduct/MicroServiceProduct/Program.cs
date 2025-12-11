using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MicroServiceProduct.Infraestructure.DataBase;
using MicroServiceProduct.Infraestructure.Repository;
using MicroServiceProduct.Domain.Services;
using MicroServiceProduct.Domain.Interfaces;
using MicroServiceProduct.Application.Services;
using System.Reflection;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Swagger con esquema Bearer
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "MicroServiceProduct API", Version = "v1" });
    // Include XML comments (requires GenerateDocumentationFile=true in csproj)
    var xmlFile = (Assembly.GetExecutingAssembly().GetName().Name ?? "") + ".xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Autenticación JWT usando el esquema Bearer. Ejemplo: Bearer {token}",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Register controllers so attribute-routed controllers are available
builder.Services.AddControllers();
builder.Services.AddAuthorization();

// Get connection string from configuration (appsettings.json)
var connectionString = builder.Configuration.GetConnectionString("MicroServiceProduct")
                       ?? builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? builder.Configuration["ConnectionStrings:MicroServiceProduct"]
                       ?? builder.Configuration["ConnectionStrings:DefaultConnection"];
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Configure ConnectionStrings.DefaultConnection o MicroServiceProduct en appsettings.json.");
}

// Register infrastructure / domain services
// Register IDataBase as a singleton using DataBaseConnection which requires an explicit connection string
builder.Services.AddSingleton<IDataBase>(_ => DataBaseConnection.GetInstance(connectionString));

// Repositories
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();

// Application services
builder.Services.AddScoped<IProductService, ProductService>();

// Configuración JWT
var issuer = builder.Configuration["Jwt:Issuer"];
var audience = builder.Configuration["Jwt:Audience"];
var key = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience) || string.IsNullOrWhiteSpace(key))
{
    throw new InvalidOperationException("Configura Jwt:Issuer, Jwt:Audience y Jwt:Key en appsettings.json.");
}

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = builder.Environment.IsDevelopment() ? false : true;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.Zero
        };

        // Diagnóstico de errores de validación
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                context.Response.Headers.Add("x-auth-error", context.Exception.GetType().Name);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                // Agregar detalle del error para depurar en Swagger
                if (!string.IsNullOrEmpty(context.Error))
                {
                    context.Response.Headers.Add("www-authenticate-error", context.Error);
                }
                if (!string.IsNullOrEmpty(context.ErrorDescription))
                {
                    context.Response.Headers.Add("www-authenticate-error-desc", context.ErrorDescription);
                }
                return Task.CompletedTask;
            }
        };
    });

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

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Map controller routes (attribute routing)
app.MapControllers().RequireAuthorization();

app.Run();
