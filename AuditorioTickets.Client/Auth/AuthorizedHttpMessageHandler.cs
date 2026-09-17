using System.Net.Http.Headers;
using Blazored.LocalStorage;

namespace AuditorioTickets.Client.Auth;

/// <summary>
/// Handler que se inserta en el pipeline del HttpClient "Api" y agrega
/// automáticamente el Bearer token a cada request, si existe uno guardado.
/// </summary>
public class AuthorizedHttpMessageHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorage;

    public AuthorizedHttpMessageHandler(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _localStorage.GetItemAsStringAsync("authToken", cancellationToken);

        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}