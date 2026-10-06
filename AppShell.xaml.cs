using ProTrack.Maui.Pages;

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
}