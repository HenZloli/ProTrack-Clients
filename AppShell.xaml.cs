using ProTrack.Maui.Pages;
using ProTrack.Maui.Services;

namespace ProTrack.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

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

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            var authService =
                Handler?.MauiContext?.Services
                    .GetService<AuthService>();

            if (authService == null)
                return;

            var restored =
                await authService.RestoreSessionAsync();

            if (restored)
            {
                await GoToAsync("HomePage");
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(
                $"Restore session lỗi: {ex}");
#endif
        }
    }
}