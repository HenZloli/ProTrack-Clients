using ProTrack.Maui.Services;

namespace ProTrack.Maui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();

            _ = CheckSessionAsync();
        }

        private async Task CheckSessionAsync()
        {
            try
            {
                // Chờ Shell khởi tạo xong
                await Task.Delay(100);

                var authService =
                    Handler?.MauiContext?.Services
                        .GetService<AuthService>();

                if (authService == null)
                    return;

                var restored =
                    await authService.RestoreSessionAsync();

                if (restored)
                {
                    await MainPage.Dispatcher.DispatchAsync(
                        async () =>
                        {
                            await Shell.Current.GoToAsync("HomePage");
                        });
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
}