using Microsoft.Maui.Storage;

namespace ProTrack.Maui;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void ConnectButton_Clicked(
        object sender,
        EventArgs e)
    {
        var serverUrl = ServerUrlEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(serverUrl))
        {
            await DisplayAlert(
                "Thông báo",
                "Mày chưa nhập địa chỉ Server.",
                "OK");

            return;
        }

        if (!Uri.TryCreate(
                serverUrl,
                UriKind.Absolute,
                out var uri))
        {
            await DisplayAlert(
                "Lỗi",
                "Địa chỉ Server không hợp lệ.",
                "OK");

            return;
        }

        if (uri.Scheme != Uri.UriSchemeHttp &&
            uri.Scheme != Uri.UriSchemeHttps)
        {
            await DisplayAlert(
                "Lỗi",
                "Server phải bắt đầu bằng http:// hoặc https://",
                "OK");

            return;
        }

        Preferences.Default.Set(
            "ServerUrl",
            serverUrl.TrimEnd('/'));

        StatusLabel.Text = "Đã lưu địa chỉ Server.";

        await DisplayAlert(
            "ProTrack",
            "Cấu hình Server thành công.",
            "OK");

        // Chuyển sang Login
        await Shell.Current.GoToAsync("LoginPage");
    }
}