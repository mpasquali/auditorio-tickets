using System.Text;
using AuditorioTickets.Api.Data;
using AuditorioTickets.Api.Services;
using MercadoPago.Config;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using AuditorioTickets.Api.BackgroundServices;
using AuditorioTickets.Api.Configuration;

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

// --- CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5289") // La URL exacta de tu Blazor
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// --- MercadoPago SDK ---
MercadoPagoConfig.AccessToken = builder.Configuration["MercadoPago:AccessToken"];

// --- CORS para el cliente Blazor WASM ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.WithOrigins(builder.Configuration["MercadoPago:FrontendBaseUrl"]!)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

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

// Aplica migraciones automáticamente al iniciar (cómodo para el MVP; en producción
// preferí ejecutar `dotnet ef database update` como paso de deploy explícito).
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
app.UseCors("PermitirFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();