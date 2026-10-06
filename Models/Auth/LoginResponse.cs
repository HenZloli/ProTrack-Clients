namespace ProTrack.Maui.Models.Auth;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;

    public DateTime TokenExpiresAt { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }

    public Guid UserId { get; set; }

    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = new();
}