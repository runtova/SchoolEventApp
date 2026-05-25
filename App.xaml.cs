using System.Windows;
using SchoolEventApp.Services;

namespace SchoolEventApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ShutdownMode = ShutdownMode.OnLastWindowClose;

            // Очищаем временные файлы отчётов прошлых сессий
            ReportService.CleanTemp();

            var login = new LoginWindow();
            login.Show();
        }
    }
}
