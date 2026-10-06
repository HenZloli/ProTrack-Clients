using ProTrack.Maui.Services;

namespace ProTrack.Maui.Pages;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();

        var authService =
            Application.Current?
                .Handler?
                .MauiContext?
                .Services
                .GetService<AuthService>();

        if (authService != null)
        {
            WelcomeLabel.Text =
                $"Xin chào, {authService.FullName}!";
        }
    }

    private async void LogoutButton_Clicked(
        object sender,
        EventArgs e)
    {
        var authService =
            Application.Current?
                .Handler?
                .MauiContext?
                .Services
                .GetService<AuthService>();

        if (authService != null)
        {
            await authService.LogoutAsync();
        }

        await Shell.Current.GoToAsync("//MainPage");
    }
}