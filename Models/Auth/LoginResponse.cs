namespace ProTrack.Maui.Models.Auth;

public class LoginResponse
{
    public string Token { get; set; } = null!;

    public string RefreshToken { get; set; } = null!;

    public DateTime TokenExpiresAt { get; set; }

    public DateTime RefreshTokenExpiresAt { get; set; }

    public Guid UserId { get; set; }

    public Guid SessionId { get; set; }

    public string Username { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public List<string> Roles { get; set; } = new();
}