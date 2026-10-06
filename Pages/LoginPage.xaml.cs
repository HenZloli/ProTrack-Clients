using ProTrack.Maui.Services;

namespace ProTrack.Maui.Pages;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _authService;

    public LoginPage(AuthService authService)
    {
        InitializeComponent();

        _authService = authService;
    }

    private async void LoginButton_Clicked(
        object sender,
        EventArgs e)
    {
        var username = UsernameEntry.Text?.Trim();
        var password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(username))
        {
            await DisplayAlert(
                "Thông báo",
                "Nhập tên đăng nhập.",
                "OK");

            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert(
                "Thông báo",
                "Nhập mật khẩu.",
                "OK");

            return;
        }

        try
        {
            SetLoading(true);

            var result =
                await _authService.LoginAsync(
                    username,
                    password);

            if (result == null)
            {
                await DisplayAlert(
                    "Đăng nhập thất bại",
                    "Tên đăng nhập hoặc mật khẩu không đúng.",
                    "OK");

                return;
            }

            await DisplayAlert(
                "Thành công",
                $"Xin chào {result.FullName}!",
                "OK");

            await Shell.Current.GoToAsync("//MainPage");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Lỗi kết nối",
                ex.Message,
                "OK");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async void RegisterButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("RegisterPage");
    }

    private void SetLoading(bool loading)
    {
        LoadingIndicator.IsVisible = loading;
        LoadingIndicator.IsRunning = loading;
    }
}