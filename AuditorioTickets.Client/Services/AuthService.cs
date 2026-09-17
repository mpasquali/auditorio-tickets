using System.Net.Http.Json;
using AuditorioTickets.Client.Auth;
using AuditorioTickets.Shared.Dtos;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace AuditorioTickets.Client.Services;

public class AuthService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authStateProvider;

    public AuthService(HttpClient http, ILocalStorageService localStorage, AuthenticationStateProvider authStateProvider)
    {
        _http = http;
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
    }

    public async Task<(bool Exito, string? Error)> LoginAsync(string usuario, string password)
    {
        var respuesta = await _http.PostAsJsonAsync("api/auth/login",
            new LoginRequestDto { Usuario = usuario, Password = password });

        if (!respuesta.IsSuccessStatusCode)
            return (false, "Usuario o contraseña incorrectos.");

        var resultado = await respuesta.Content.ReadFromJsonAsync<LoginResponseDto>();

        // 1) Persistimos el token
        await _localStorage.SetItemAsStringAsync("authToken", resultado!.Token);

        // 2) Avisamos al provider que el estado de auth cambió, para que Blazor
        //    re-renderice todo lo que depende de <AuthorizeView>/[Authorize] YA,
        //    sin esperar a la próxima navegación.
        ((CustomAuthStateProvider)_authStateProvider).NotificarUsuarioAutenticado(resultado.Token);

        return (true, null);
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync("authToken");
        ((CustomAuthStateProvider)_authStateProvider).NotificarLogout();
    }
}