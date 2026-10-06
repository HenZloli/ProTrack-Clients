using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Maui.Storage;
using ProTrack.Maui.Models.Auth;

namespace ProTrack.Maui.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;

    // =========================================================
    // HEARTBEAT INTERVAL
    // =========================================================

#if DEBUG

    // Khi Debug:
    // Heartbeat mỗi 10 giây để dễ test.
    private static readonly TimeSpan HeartbeatInterval =
        TimeSpan.FromSeconds(10);

    // Debug:
    // Mất kết nối quá 30 giây => logout.
    private static readonly TimeSpan MaxConnectionLoss =
        TimeSpan.FromSeconds(30);

#else

    // Khi Release:
    // Heartbeat mỗi 5 phút.
    private static readonly TimeSpan HeartbeatInterval =
        TimeSpan.FromMinutes(5);

    // Release:
    // Mất kết nối quá 30 phút => logout.
    private static readonly TimeSpan MaxConnectionLoss =
        TimeSpan.FromMinutes(30);

#endif

    // =========================================================
    // SECURE STORAGE KEYS
    // =========================================================

    private const string AccessTokenKey =
        "ProTrack_AccessToken";

    private const string RefreshTokenKey =
        "ProTrack_RefreshToken";

    private const string UserIdKey =
        "ProTrack_UserId";

    private const string SessionIdKey =
        "ProTrack_SessionId";

    private const string UsernameKey =
        "ProTrack_Username";

    private const string FullNameKey =
        "ProTrack_FullName";

    private const string RolesKey =
        "ProTrack_Roles";

    // =========================================================
    // HEARTBEAT STATE
    // =========================================================

    private CancellationTokenSource? _heartbeatCts;

    private Task? _heartbeatTask;

    private bool _isHeartbeatRunning;

    // Thời điểm bắt đầu mất kết nối.
    private DateTime? _connectionLostAt;

    // Mốc cuối cùng đã log.
    // Ví dụ:
    // 10 -> đã log 10 giây
    // 20 -> đã log 20 giây
    // 30 -> đã log 30 giây
    private int _lastConnectionMilestone;

    // =========================================================
    // EVENTS
    // =========================================================

    // Server/session không còn hợp lệ.
    //
    // Ví dụ:
    // - 401
    // - 403
    // - IsActive = false
    public event EventHandler? SessionInvalidated;

    // Không thể kết nối Server trong thời gian cho phép.
    //
    // Debug:
    // 30 giây
    //
    // Release:
    // 30 phút
    public event EventHandler? ConnectionLost;

    // =========================================================
    // USER SESSION
    // =========================================================

    public string? AccessToken
    {
        get;
        private set;
    }

    public string? RefreshToken
    {
        get;
        private set;
    }

    public Guid UserId
    {
        get;
        private set;
    }

    public Guid SessionId
    {
        get;
        private set;
    }

    public string Username
    {
        get;
        private set;
    } = string.Empty;

    public string FullName
    {
        get;
        private set;
    } = string.Empty;

    public List<string> Roles
    {
        get;
        private set;
    } = new();

    public bool IsLoggedIn =>
        !string.IsNullOrWhiteSpace(AccessToken);

    // =========================================================
    // SERVER URL
    // =========================================================

    private string ServerUrl
    {
        get
        {
            var url =
                Preferences.Default.Get(
                    "ServerUrl",
                    "http://localhost:5081");

            return url.TrimEnd('/');
        }
    }

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public AuthService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    // =========================================================
    // LOGIN
    // =========================================================

    public async Task<LoginResponse?> LoginAsync(
        string username,
        string password)
    {
#if DEBUG
        System.Diagnostics.Debug.WriteLine(
            $"[{DateTime.Now:HH:mm:ss}] 🔐 Đang đăng nhập...");
#endif

        var request = new LoginRequest
        {
            Username = username,
            Password = password
        };

        var response =
            await _httpClient.PostAsJsonAsync(
                $"{ServerUrl}/api/Auth/login",
                request);

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content
                    .ReadAsStringAsync();

            throw new Exception(
                $"Server trả về {(int)response.StatusCode}:\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>();

        if (result == null)
            return null;

        await SaveSessionAsync(result);

        // Reset trạng thái connection.
        ResetConnectionState();

        // Bắt đầu heartbeat.
        StartHeartbeat();

#if DEBUG
        System.Diagnostics.Debug.WriteLine(
            $"[{DateTime.Now:HH:mm:ss}] ✅ Đăng nhập thành công.");
#endif

        return result;
    }

    // =========================================================
    // REGISTER
    // =========================================================

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

        var response =
            await _httpClient.PostAsJsonAsync(
                $"{ServerUrl}/api/Auth/register",
                request);

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content
                    .ReadAsStringAsync();

            throw new Exception(
                $"Server trả về {(int)response.StatusCode}:\n{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>();

        if (result == null)
            return null;

        await SaveSessionAsync(result);

        /*
         * Register hiện tại backend trả SessionId = Guid.Empty
         * nên KHÔNG start heartbeat ở đây.
         *
         * Khi đăng nhập thành công thì LoginAsync sẽ start.
         */

        return result;
    }

    // =========================================================
    // SAVE SESSION
    // =========================================================

    private async Task SaveSessionAsync(
        LoginResponse response)
    {
        AccessToken =
            response.Token;

        RefreshToken =
            response.RefreshToken;

        UserId =
            response.UserId;

        SessionId =
            response.SessionId;

        Username =
            response.Username;

        FullName =
            response.FullName;

        Roles =
            response.Roles ?? new List<string>();

        // -----------------------------------------------------
        // Access Token
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(
                AccessToken))
        {
            await SecureStorage.Default.SetAsync(
                AccessTokenKey,
                AccessToken);
        }

        // -----------------------------------------------------
        // Refresh Token
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(
                RefreshToken))
        {
            await SecureStorage.Default.SetAsync(
                RefreshTokenKey,
                RefreshToken);
        }

        // -----------------------------------------------------
        // User ID
        // -----------------------------------------------------

        await SecureStorage.Default.SetAsync(
            UserIdKey,
            UserId.ToString());

        // -----------------------------------------------------
        // Session ID
        // -----------------------------------------------------

        await SecureStorage.Default.SetAsync(
            SessionIdKey,
            SessionId.ToString());

        // -----------------------------------------------------
        // Username
        // -----------------------------------------------------

        await SecureStorage.Default.SetAsync(
            UsernameKey,
            Username);

        // -----------------------------------------------------
        // Full Name
        // -----------------------------------------------------

        await SecureStorage.Default.SetAsync(
            FullNameKey,
            FullName);

        // -----------------------------------------------------
        // Roles
        // -----------------------------------------------------

        await SecureStorage.Default.SetAsync(
            RolesKey,
            string.Join(",", Roles));
    }

    // =========================================================
    // RESTORE SESSION
    // =========================================================

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

            var sessionId =
                await SecureStorage.Default.GetAsync(
                    SessionIdKey);

            var username =
                await SecureStorage.Default.GetAsync(
                    UsernameKey);

            var fullName =
                await SecureStorage.Default.GetAsync(
                    FullNameKey);

            var roles =
                await SecureStorage.Default.GetAsync(
                    RolesKey);

            // -------------------------------------------------
            // Không có token
            // -------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    accessToken))
            {
                return false;
            }

            // -------------------------------------------------
            // Không có SessionId
            // -------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    sessionId))
            {
                return false;
            }

            // -------------------------------------------------
            // Parse UserId
            // -------------------------------------------------

            if (!Guid.TryParse(
                    userId,
                    out var parsedUserId))
            {
                return false;
            }

            // -------------------------------------------------
            // Parse SessionId
            // -------------------------------------------------

            if (!Guid.TryParse(
                    sessionId,
                    out var parsedSessionId))
            {
                return false;
            }

            // -------------------------------------------------
            // Restore
            // -------------------------------------------------

            AccessToken =
                accessToken;

            RefreshToken =
                refreshToken;

            UserId =
                parsedUserId;

            SessionId =
                parsedSessionId;

            Username =
                username ?? string.Empty;

            FullName =
                fullName ?? string.Empty;

            Roles =
                string.IsNullOrWhiteSpace(roles)
                    ? new List<string>()
                    : roles
                        .Split(
                            ',',
                            StringSplitOptions.RemoveEmptyEntries)
                        .ToList();

            // Reset trạng thái connection.
            ResetConnectionState();

            // -------------------------------------------------
            // Session tồn tại
            // -------------------------------------------------

            StartHeartbeat();

#if DEBUG
            System.Diagnostics.Debug.WriteLine(
                $"[{DateTime.Now:HH:mm:ss}] 🔄 Khôi phục session thành công.");
#endif

            return true;
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(
                $"[{DateTime.Now:HH:mm:ss}] ❌ Restore session lỗi: {ex.Message}");
#endif

            return false;
        }
    }

    // =========================================================
    // HEARTBEAT - MANUAL
    // =========================================================

    public async Task<bool> HeartbeatAsync()
    {
        try
        {
            // -------------------------------------------------
            // Không có Access Token
            // -------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    AccessToken))
            {
                return false;
            }

            // -------------------------------------------------
            // Không có UserId
            // -------------------------------------------------

            if (UserId == Guid.Empty)
            {
                return false;
            }

            // -------------------------------------------------
            // Không có SessionId
            // -------------------------------------------------

            if (SessionId == Guid.Empty)
            {
                return false;
            }

#if DEBUG
            System.Diagnostics.Debug.WriteLine(
                $"[{DateTime.Now:HH:mm:ss}] ❤️ Heartbeat đang kiểm tra Server...");
#endif

            // -------------------------------------------------
            // Tạo request
            // -------------------------------------------------

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{ServerUrl}/api/Auth/heartbeat");

            // -------------------------------------------------
            // JWT Bearer
            // -------------------------------------------------

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    AccessToken);

            // -------------------------------------------------
            // Body
            // -------------------------------------------------

            request.Content =
                JsonContent.Create(new
                {
                    UserId = UserId,
                    SessionId = SessionId
                });

            // -------------------------------------------------
            // Gửi request
            // -------------------------------------------------

            var response =
                await _httpClient.SendAsync(
                    request);

            // =================================================
            // SERVER REJECT
            // =================================================

            if (!response.IsSuccessStatusCode)
            {
                var statusCode =
                    (int)response.StatusCode;

#if DEBUG
                System.Diagnostics.Debug.WriteLine(
                    $"[{DateTime.Now:HH:mm:ss}] ❌ Heartbeat thất bại: HTTP {statusCode}");
#endif

                // -------------------------------------------------
                // 401 / 403
                // Session hoặc token không còn hợp lệ.
                // -------------------------------------------------

                if (response.StatusCode ==
                        HttpStatusCode.Unauthorized ||
                    response.StatusCode ==
                        HttpStatusCode.Forbidden)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine(
                        $"[{DateTime.Now:HH:mm:ss}] 🚪 Session không hợp lệ → Logout.");
#endif

                    StopHeartbeat();

                    await ClearLocalSessionAsync();

                    SessionInvalidated?.Invoke(
                        this,
                        EventArgs.Empty);

                    return false;
                }

                // -------------------------------------------------
                // Các lỗi HTTP khác:
                // Coi như Server/network đang có vấn đề.
                // -------------------------------------------------

                await HandleConnectionFailureAsync();

                return false;
            }

            // -------------------------------------------------
            // Đọc response
            // -------------------------------------------------

            var result =
                await response.Content
                    .ReadFromJsonAsync<HeartbeatResponse>();

            if (result == null)
            {
                await HandleConnectionFailureAsync();

                return false;
            }

            // -------------------------------------------------
            // Server báo session không active
            // -------------------------------------------------

            if (!result.IsActive)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(
                    $"[{DateTime.Now:HH:mm:ss}] ❌ Server báo Session không active.");
#endif

                StopHeartbeat();

                await ClearLocalSessionAsync();

                SessionInvalidated?.Invoke(
                    this,
                    EventArgs.Empty);

                return false;
            }

            // =================================================
            // HEARTBEAT THÀNH CÔNG
            // =================================================

            // Server đã kết nối lại.
            if (_connectionLostAt != null)
            {
#if DEBUG
                var lostDuration =
                    DateTime.UtcNow -
                    _connectionLostAt.Value;

                System.Diagnostics.Debug.WriteLine(
                    $"[{DateTime.Now:HH:mm:ss}] ✅ Server đã kết nối lại " +
                    $"sau {lostDuration.TotalSeconds:F0} giây → Reset bộ đếm.");
#endif

                ResetConnectionState();
            }
            else
            {
                ResetConnectionState();
            }

#if DEBUG
            System.Diagnostics.Debug.WriteLine(
                $"[{DateTime.Now:HH:mm:ss}] ❤️ Heartbeat OK - " +
                $"ServerTime: {result.ServerTime:HH:mm:ss}");
#endif

            return true;
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(
                $"[{DateTime.Now:HH:mm:ss}] ❌ Heartbeat lỗi kết nối: " +
                $"{ex.Message}");
#endif

            // Lỗi mạng:
            // KHÔNG logout ngay.
            await HandleConnectionFailureAsync();

            return false;
        }
    }

    // =========================================================
    // HANDLE CONNECTION FAILURE
    // =========================================================

    private async Task HandleConnectionFailureAsync()
    {
        // -----------------------------------------------------
        // Lần đầu mất kết nối
        // -----------------------------------------------------

        if (_connectionLostAt == null)
        {
            _connectionLostAt =
                DateTime.UtcNow;

            _lastConnectionMilestone = 0;

#if DEBUG
            System.Diagnostics.Debug.WriteLine(
                $"[{DateTime.Now:HH:mm:ss}] ⚠️ SERVER MẤT KẾT NỐI.");
#endif
        }

        // -----------------------------------------------------
        // Tính thời gian mất kết nối
        // -----------------------------------------------------

        var elapsed =
            DateTime.UtcNow -
            _connectionLostAt.Value;

        var elapsedSeconds =
            (int)elapsed.TotalSeconds;

        var maxSeconds =
            (int)MaxConnectionLoss.TotalSeconds;

        // -----------------------------------------------------
        // Tính mốc 10 / 20 / 30...
        // -----------------------------------------------------

        var currentMilestone =
            (elapsedSeconds / 10) * 10;

        // Không cho vượt quá timeout.
        if (currentMilestone > maxSeconds)
        {
            currentMilestone =
                maxSeconds;
        }

        // -----------------------------------------------------
        // Log từng mốc, mỗi mốc chỉ 1 lần
        // -----------------------------------------------------

        if (currentMilestone >= 10 &&
            currentMilestone > _lastConnectionMilestone)
        {
            _lastConnectionMilestone =
                currentMilestone;

#if DEBUG
            System.Diagnostics.Debug.WriteLine(
                $"[{DateTime.Now:HH:mm:ss}] ⚠️ Mất kết nối " +
                $"{currentMilestone}/{maxSeconds} giây.");
#endif
        }

        // -----------------------------------------------------
        // Quá thời gian cho phép
        // -----------------------------------------------------

        if (elapsed >= MaxConnectionLoss)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(
                $"[{DateTime.Now:HH:mm:ss}] 🚪 Đã mất kết nối quá " +
                $"{maxSeconds} giây → LOGOUT.");
#endif

            StopHeartbeat();

            await ClearLocalSessionAsync();

            ConnectionLost?.Invoke(
                this,
                EventArgs.Empty);
        }
    }

    // =========================================================
    // RESET CONNECTION STATE
    // =========================================================

    private void ResetConnectionState()
    {
        _connectionLostAt = null;

        _lastConnectionMilestone = 0;
    }

    // =========================================================
    // START HEARTBEAT
    // =========================================================

    public void StartHeartbeat()
    {
        // Đã chạy rồi.
        if (_isHeartbeatRunning)
            return;

        // Không có session.
        if (!IsLoggedIn)
            return;

        if (UserId == Guid.Empty)
            return;

        if (SessionId == Guid.Empty)
            return;

        ResetConnectionState();

        _heartbeatCts =
            new CancellationTokenSource();

        _isHeartbeatRunning = true;

#if DEBUG
        System.Diagnostics.Debug.WriteLine(
            $"[{DateTime.Now:HH:mm:ss}] ▶ Heartbeat START - " +
            $"Interval: {HeartbeatInterval.TotalSeconds}s - " +
            $"Timeout: {MaxConnectionLoss.TotalSeconds}s");
#endif

        _heartbeatTask =
            HeartbeatLoopAsync(
                _heartbeatCts.Token);
    }

    // =========================================================
    // HEARTBEAT LOOP
    // =========================================================

    private async Task HeartbeatLoopAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(
                    HeartbeatInterval,
                    cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    break;

                if (!IsLoggedIn)
                    break;

                var success =
                    await HeartbeatAsync();

#if DEBUG
                System.Diagnostics.Debug.WriteLine(
                    $"[{DateTime.Now:HH:mm:ss}] Heartbeat result: {success}");
#endif
            }
        }
        catch (TaskCanceledException)
        {
            // Bình thường khi StopHeartbeat().
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(
                $"[{DateTime.Now:HH:mm:ss}] ❌ Heartbeat loop lỗi: {ex}");
#endif
        }
        finally
        {
            _isHeartbeatRunning = false;

            _heartbeatTask = null;

            _heartbeatCts?.Dispose();

            _heartbeatCts = null;
        }
    }

    // =========================================================
    // STOP HEARTBEAT
    // =========================================================

    public void StopHeartbeat()
    {
        if (!_isHeartbeatRunning &&
            _heartbeatCts == null)
        {
            return;
        }

        try
        {
            _heartbeatCts?.Cancel();
        }
        catch
        {
            // Ignore.
        }

        _isHeartbeatRunning = false;

#if DEBUG
        System.Diagnostics.Debug.WriteLine(
            $"[{DateTime.Now:HH:mm:ss}] ⏹ Heartbeat STOP.");
#endif
    }

    // =========================================================
    // CLEAR LOCAL SESSION
    // =========================================================

    private async Task ClearLocalSessionAsync()
    {
        // -----------------------------------------------------
        // Dừng heartbeat
        // -----------------------------------------------------

        StopHeartbeat();

        // -----------------------------------------------------
        // Reset connection state
        // -----------------------------------------------------

        ResetConnectionState();

        // -----------------------------------------------------
        // Clear memory
        // -----------------------------------------------------

        AccessToken = null;

        RefreshToken = null;

        UserId = Guid.Empty;

        SessionId = Guid.Empty;

        Username = string.Empty;

        FullName = string.Empty;

        Roles.Clear();

        // -----------------------------------------------------
        // Clear SecureStorage
        // -----------------------------------------------------

        SecureStorage.Default.Remove(
            AccessTokenKey);

        SecureStorage.Default.Remove(
            RefreshTokenKey);

        SecureStorage.Default.Remove(
            UserIdKey);

        SecureStorage.Default.Remove(
            SessionIdKey);

        SecureStorage.Default.Remove(
            UsernameKey);

        SecureStorage.Default.Remove(
            FullNameKey);

        SecureStorage.Default.Remove(
            RolesKey);

        await Task.CompletedTask;
    }

    // =========================================================
    // LOGOUT
    // =========================================================

    public async Task LogoutAsync()
    {
#if DEBUG
        System.Diagnostics.Debug.WriteLine(
            $"[{DateTime.Now:HH:mm:ss}] 🚪 Logout thủ công.");
#endif

        await ClearLocalSessionAsync();
    }
}
