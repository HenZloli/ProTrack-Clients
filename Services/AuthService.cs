using System.Net.Http.Json;
using ProTrack.Maui.Models.Auth;

namespace ProTrack.Maui.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;

    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }

    public Guid UserId { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;

    public List<string> Roles { get; private set; } = new();

    public bool IsLoggedIn =>
        !string.IsNullOrWhiteSpace(AccessToken);

    public AuthService(string serverUrl)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(
                serverUrl.TrimEnd('/') + "/")
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
            return null;

        var result =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>();

        if (result == null)
            return null;

        SaveSession(result);

        return result;
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
            return null;

        var result =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>();

        if (result == null)
            return null;

        SaveSession(result);

        return result;
    }

    private void SaveSession(LoginResponse response)
    {
        AccessToken = response.Token;
        RefreshToken = response.RefreshToken;

        UserId = response.UserId;
        Username = response.Username;
        FullName = response.FullName;

        Roles = response.Roles;
    }

    public void Logout()
    {
        AccessToken = null;
        RefreshToken = null;

        UserId = Guid.Empty;
        Username = string.Empty;
        FullName = string.Empty;

        Roles.Clear();
    }
}