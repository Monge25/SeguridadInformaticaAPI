using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SeguridadInformaticaAPI.Custom;
using SeguridadInformaticaAPI.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Puerto para Render
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<SeguridadInformaticaDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddSingleton<Utilities>();

// ===== JWT desde cookie HttpOnly =====
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true;
    options.SaveToken = true;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
        )
    };

    // EVENTOS PARA CONTROLAR EXACTAMENTE DE DÓNDE SALE EL TOKEN 
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Evitar que tome un token del header Authorization
            if (context.Request.Headers.ContainsKey("Authorization"))
            {
                context.NoResult(); // Ignorar por completo este token
                return Task.CompletedTask;
            }

            // Leer SOLO desde cookie "jwt"
            if (!context.Request.Cookies.TryGetValue("jwt", out var token) || string.IsNullOrEmpty(token))
            {
                context.NoResult(); // Sin cookie SIN token debe dar 401
                return Task.CompletedTask;
            }

            // Cookie encontrada asignamos token
            context.Token = token;
            return Task.CompletedTask;
        }
    };
});

// CORS 
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "https://seguridad-informatica-web.vercel.app",
                "http://localhost:3000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// ===== Middleware =====
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
