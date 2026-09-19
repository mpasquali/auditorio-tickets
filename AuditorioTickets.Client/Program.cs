using AuditorioTickets.Client;
using AuditorioTickets.Client.Auth;
using AuditorioTickets.Client.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// En desarrollo usa appsettings.json local; en producción en Render fuerza la URL de la API
string apiBaseUrl = builder.HostEnvironment.IsDevelopment()
    ? (builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5219/")
    : "https://auditorio-tickets.onrender.com/";

// Asegurar que siempre termine en '/' para que HttpClient resuelva bien las rutas relativas
if (!apiBaseUrl.EndsWith("/"))
{
    apiBaseUrl += "/";
}

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<AuthorizedHttpMessageHandler>();

builder.Services
    .AddHttpClient("Api", client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthorizedHttpMessageHandler>();

// Cualquier componente o servicio que inyecte HttpClient recibe el cliente configurado
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Api"));

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<EventoApiService>();
builder.Services.AddScoped<BoletoApiService>();

await builder.Build().RunAsync();