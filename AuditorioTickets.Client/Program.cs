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

// Cambiado para que apunte directamente a tu API en Render si no encuentra la variable de configuración
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://tu-api-en-render.onrender.com";

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<AuthorizedHttpMessageHandler>();

builder.Services
    .AddHttpClient("Api", client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthorizedHttpMessageHandler>();

// Cualquier componente que inyecte HttpClient recibe el cliente "Api" ya configurado.
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Api"));

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<EventoApiService>();
builder.Services.AddScoped<BoletoApiService>();

await builder.Build().RunAsync();