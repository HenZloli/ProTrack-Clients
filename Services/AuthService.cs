using System.Net.Http.Json;
using Microsoft.Maui.Storage;
using ProTrack.Maui.Models.Auth;

namespace ProTrack.Maui.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;

    private const string AccessTokenKey = "ProTrack_AccessToken";
    private const string RefreshTokenKey = "ProTrack_RefreshToken";
    private const string UserIdKey = "ProTrack_UserId";
    private const string UsernameKey = "ProTrack_Username";
    private const string FullNameKey = "ProTrack_FullName";
    private const string RolesKey = "ProTrack_Roles";

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

    // =========================
    // LOGIN
    // =========================

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
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Server trả về {(int)response.StatusCode}:\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>();

        if (result == null)
            return null;

        await SaveSessionAsync(result);

        return result;
    }

    // =========================
    // REGISTER
    // =========================

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
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Server trả về {(int)response.StatusCode}:\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>();

        if (result == null)
            return null;

        await SaveSessionAsync(result);

        return result;
    }

    // =========================
    // SAVE SESSION
    // =========================

    private async Task SaveSessionAsync(
        LoginResponse response)
    {
        AccessToken = response.Token;
        RefreshToken = response.RefreshToken;

        UserId = response.UserId;
        Username = response.Username;
        FullName = response.FullName;
        Roles = response.Roles ?? new List<string>();

        await SecureStorage.Default.SetAsync(
            AccessTokenKey,
            AccessToken);

        await SecureStorage.Default.SetAsync(
            RefreshTokenKey,
            RefreshToken);

        await SecureStorage.Default.SetAsync(
            UserIdKey,
            UserId.ToString());

        await SecureStorage.Default.SetAsync(
            UsernameKey,
            Username);

        await SecureStorage.Default.SetAsync(
            FullNameKey,
            FullName);

        await SecureStorage.Default.SetAsync(
            RolesKey,
            string.Join(",", Roles));
    }

    // =========================
    // RESTORE SESSION
    // =========================

    public async Task<bool> RestoreSessionAsync()
    {
        try
        {
            var accessToken =
                await SecureStorage.Default.GetAsync(
                    AccessTokenKey);

            var refreshToken =
                await SecureStorage.Default.GetAsync(
                    RefreshTokenKey);

            var userId =
                await SecureStorage.Default.GetAsync(
                    UserIdKey);

            var username =
                await SecureStorage.Default.GetAsync(
                    UsernameKey);

            var fullName =
                await SecureStorage.Default.GetAsync(
                    FullNameKey);

            var roles =
                await SecureStorage.Default.GetAsync(
                    RolesKey);

            if (string.IsNullOrWhiteSpace(accessToken))
                return false;

            AccessToken = accessToken;
            RefreshToken = refreshToken;

            if (Guid.TryParse(userId, out var parsedUserId))
                UserId = parsedUserId;

            Username = username ?? string.Empty;
            FullName = fullName ?? string.Empty;

            Roles = string.IsNullOrWhiteSpace(roles)
                ? new List<string>()
                : roles
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

            return true;
        }
        catch
        {
            return false;
        }
    }

    // =========================
    // LOGOUT
    // =========================

    public async Task LogoutAsync()
    {
        AccessToken = null;
        RefreshToken = null;

        UserId = Guid.Empty;
        Username = string.Empty;
        FullName = string.Empty;
        Roles.Clear();

        SecureStorage.Default.Remove(
            AccessTokenKey);

        SecureStorage.Default.Remove(
            RefreshTokenKey);

        SecureStorage.Default.Remove(
            UserIdKey);

        SecureStorage.Default.Remove(
            UsernameKey);

        SecureStorage.Default.Remove(
            FullNameKey);

        SecureStorage.Default.Remove(
            RolesKey);

        await Task.CompletedTask;
    }
}