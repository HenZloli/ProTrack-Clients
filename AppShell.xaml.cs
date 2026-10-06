using ProTrack.Maui.Pages;
using ProTrack.Maui.Services;

namespace ProTrack.Maui;

public partial class AppShell : Shell
{
    private AuthService? _authService;

    private bool _sessionEventsRegistered;

    private bool _isNavigatingToLogin;

    public AppShell()
    {
        InitializeComponent();

        // =====================================================
        // REGISTER ROUTES
        // =====================================================

        Routing.RegisterRoute(
            "LoginPage",
            typeof(LoginPage));

        Routing.RegisterRoute(
            "RegisterPage",
            typeof(RegisterPage));

        Routing.RegisterRoute(
            "HomePage",
            typeof(HomePage));
    }

    // =========================================================
    // SHELL APPEARING
    // =========================================================

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            _authService =
                Handler?
                    .MauiContext?
                    .Services
                    .GetService<AuthService>();

            if (_authService == null)
                return;

            // Chỉ đăng ký event 1 lần
            if (!_sessionEventsRegistered)
            {
                _authService.SessionInvalidated +=
                    AuthService_SessionInvalidated;

                _authService.ConnectionLost +=
                    AuthService_ConnectionLost;

                _sessionEventsRegistered = true;
            }

            // =================================================
            // RESTORE SESSION
            // =================================================

            var restored =
                await _authService.RestoreSessionAsync();

            if (restored)
            {
                // Có session local
                // → vào Home
                await GoToAsync("HomePage");
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(
                $"AppShell restore lỗi: {ex}");
#endif
        }
    }

    // =========================================================
    // SERVER REVOKE / SESSION INVALID
    // =========================================================

    private async void AuthService_SessionInvalidated(
        object? sender,
        EventArgs e)
    {
        await NavigateToLoginAsync(
            "Phiên đăng nhập không còn hợp lệ.\n" +
            "Vui lòng đăng nhập lại.");
    }

    // =========================================================
    // CONNECTION LOST TOO LONG
    // =========================================================

    private async void AuthService_ConnectionLost(
        object? sender,
        EventArgs e)
    {
        await NavigateToLoginAsync(
            "Không thể duy trì kết nối với Server " +
            "trong thời gian cho phép.\n" +
            "Vui lòng đăng nhập lại.");
    }

    // =========================================================
    // NAVIGATE LOGIN
    // =========================================================

    private async Task NavigateToLoginAsync(
        string message)
    {
        if (_isNavigatingToLogin)
            return;

        _isNavigatingToLogin = true;

        try
        {
            await MainThread.InvokeOnMainThreadAsync(
                async () =>
                {
                    try
                    {
                        await DisplayAlert(
                            "Phiên đăng nhập",
                            message,
                            "OK");

                        /*
                         * LoginPage là route đăng ký bằng
                         * Routing.RegisterRoute nên dùng
                         * relative route.
                         */
                        await GoToAsync(
                            "LoginPage",
                            false);
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine(
                            $"Navigate Login lỗi: {ex}");
#endif
                    }
                });
        }
        finally
        {
            _isNavigatingToLogin = false;
        }
    }
}
