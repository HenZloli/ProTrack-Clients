using ProTrack.Maui.Services;

namespace ProTrack.Maui.Pages;

public partial class RegisterPage : ContentPage
{
    private readonly AuthService _authService;

    public RegisterPage(AuthService authService)
    {
        InitializeComponent();

        _authService = authService;
    }

    private async void RegisterButton_Clicked(
        object sender,
        EventArgs e)
    {
        var fullName = FullNameEntry.Text?.Trim();
        var username = UsernameEntry.Text?.Trim();
        var email = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text;
        var confirmPassword = ConfirmPasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(fullName))
        {
            await DisplayAlert(
                "Thông báo",
                "Nhập họ và tên.",
                "OK");

            return;
        }

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

        if (password != confirmPassword)
        {
            await DisplayAlert(
                "Thông báo",
                "Mật khẩu nhập lại không khớp.",
                "OK");

            return;
        }

        try
        {
            SetLoading(true);

            var result =
                await _authService.RegisterAsync(
                    username,
                    password,
                    fullName,
                    email);

            if (result == null)
            {
                await DisplayAlert(
                    "Đăng ký thất bại",
                    "Không thể tạo tài khoản. Kiểm tra username/email có thể đã tồn tại.",
                    "OK");

                return;
            }

            await DisplayAlert(
                "Đăng ký thành công",
                $"Chào mừng {result.FullName} đến với ProTrack.",
                "OK");

            await Shell.Current.GoToAsync("//MainPage");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Lỗi",
                ex.Message,
                "OK");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void SetLoading(bool loading)
    {
        LoadingIndicator.IsVisible = loading;
        LoadingIndicator.IsRunning = loading;
    }
}