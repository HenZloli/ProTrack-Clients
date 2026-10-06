namespace ProTrack.Maui.Models.Auth;

public class HeartbeatResponse
{
    public bool IsActive { get; set; }

    public DateTime ServerTime { get; set; }

    public DateTime SessionExpiresAt { get; set; }
}