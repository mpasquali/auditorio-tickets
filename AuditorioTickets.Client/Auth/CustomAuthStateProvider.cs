using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace AuditorioTickets.Client.Auth;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private static readonly ClaimsPrincipal Anonimo = new(new ClaimsIdentity());

    public CustomAuthStateProvider(ILocalStorageService localStorage) => _localStorage = localStorage;

    // Blazor llama esto automáticamente al arrancar la app y cada vez que
    // NotifyAuthenticationStateChanged se dispara.
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _localStorage.GetItemAsStringAsync("authToken");
        if (string.IsNullOrWhiteSpace(token))
            return new AuthenticationState(Anonimo);

        try
        {
            var claims = ParsearClaimsDelJwt(token).ToList();

            // Chequeo de expiración del lado cliente (el backend igual la re-valida en cada request).
            var exp = claims.FirstOrDefault(c => c.Type == "exp");
            if (exp is not null && DateTimeOffset.FromUnixTimeSeconds(long.Parse(exp.Value)) < DateTimeOffset.UtcNow)
            {
                await _localStorage.RemoveItemAsync("authToken");
                return new AuthenticationState(Anonimo);
            }

            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt")));
        }
        catch
        {
            return new AuthenticationState(Anonimo);
        }
    }

    public void NotificarUsuarioAutenticado(string token)
    {
        var identity = new ClaimsIdentity(ParsearClaimsDelJwt(token), "jwt");
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity))));
    }

    public void NotificarLogout() =>
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonimo)));

    private static IEnumerable<Claim> ParsearClaimsDelJwt(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var json = JsonSerializer.Deserialize<Dictionary<string, object>>(ParseBase64WithoutPadding(payload))!;

        foreach (var kvp in json)
        {
            var tipo = kvp.Key.EndsWith("/role", StringComparison.OrdinalIgnoreCase) ? ClaimTypes.Role
                     : kvp.Key.EndsWith("/name", StringComparison.OrdinalIgnoreCase) ? ClaimTypes.Name
                     : kvp.Key;

            if (kvp.Value is JsonElement { ValueKind: JsonValueKind.Array } arr)
                foreach (var item in arr.EnumerateArray())
                    yield return new Claim(tipo, item.ToString());
            else
                yield return new Claim(tipo, kvp.Value?.ToString() ?? string.Empty);
        }
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        base64 = base64.Replace('-', '+').Replace('_', '/');
        base64 += (base64.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
        return Convert.FromBase64String(base64);
    }
}