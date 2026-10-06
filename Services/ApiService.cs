using System.Net.Http.Headers;
using System.Net.Http.Json;
using ProTrack.Maui.Models.Auth;

namespace ProTrack.Maui.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;

    public ApiService(string serverUrl)
    {
        serverUrl = serverUrl.TrimEnd('/');

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(serverUrl + "/")
        };
    }

    public async Task<LoginResponse?> LoginAsync(
        string username,
        string password)
    {
        var request = new LoginRequest
        {
            Username = username,
            Password = password
        };

        var response = await _httpClient.PostAsJsonAsync(
            "api/Auth/login",
            request);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content
            .ReadFromJsonAsync<LoginResponse>();
    }

    public async Task<LoginResponse?> RegisterAsync(
        string username,
        string password,
        string fullName,
        string? email)
    {
        var request = new RegisterRequest
        {
            Username = username,
            Password = password,
            FullName = fullName,
            Email = email
        };

        var response = await _httpClient.PostAsJsonAsync(
            "api/Auth/register",
            request);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content
            .ReadFromJsonAsync<LoginResponse>();
    }

    public async Task<bool> CheckServerAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(
                "api/health");

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public void SetAccessToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);
    }
}