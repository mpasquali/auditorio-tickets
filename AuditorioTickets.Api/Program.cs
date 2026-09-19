using System.Text;
using AuditorioTickets.Api.BackgroundServices;
using AuditorioTickets.Api.Configuration;
using AuditorioTickets.Api.Data;
using AuditorioTickets.Api.Services;
using MercadoPago.Config;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// --- Base de datos ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- Servicios propios ---
builder.Services.AddScoped<IEventoService, EventoService>();
builder.Services.AddScoped<IMercadoPagoService, MercadoPagoService>();
builder.Services.AddScoped<IQrService, QrService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.Configure<ExpiracionBoletosOptions>(builder.Configuration.GetSection("ExpiracionBoletos"));
builder.Services.AddHostedService<ExpiracionBoletosService>();

// --- CORS unificado ---
const string CorsPolicy = "AllowAppOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        var frontendUrl = builder.Configuration["MercadoPago:FrontendBaseUrl"];

        var origins = new List<string>
        {
            "http://localhost:5289",
            "https://localhost:7198",
            "https://auditorio-tickets-1.onrender.com"
        };

        if (!string.IsNullOrWhiteSpace(frontendUrl))
        {
            origins.Add(frontendUrl.TrimEnd('/'));
        }

        policy.WithOrigins(origins.ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// --- MercadoPago SDK ---
MercadoPagoConfig.AccessToken = builder.Configuration["MercadoPago:AccessToken"];

// --- Autenticación JWT ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Aplicar migraciones pendientes
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS debe ejecutarse antes de Authentication y Authorization
app.UseCors(CorsPolicy);

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();