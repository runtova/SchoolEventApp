#nullable enable
using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;

namespace SchoolEventApp
{
    public partial class BackupWindow : Window
    {
        private static readonly string ConnectionString =
            ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString
            ?? "Server=.\\SQLEXPRESS;Database=School_event;Integrated Security=True;TrustServerCertificate=True;";

        private static string DatabaseName => ExtractValue(ConnectionString, "Database");
        private static string ServerName   => ExtractValue(ConnectionString, "Server");

        public BackupWindow()
        {
            InitializeComponent();

            // Папка по умолчанию — стандартная папка бэкапов SQL Server.
            // SQL Server имеет права на запись туда гарантированно.
            string defaultFolder = GetSqlServerBackupFolder();
            tbBackupFolder.Text = defaultFolder;
        }

        // ── Получить стандартную папку бэкапов SQL Server ───────────────────
        // SQL Server пишет туда сам, значит права есть всегда.
        private static string GetSqlServerBackupFolder()
        {
            try
            {
                string sql = @"
EXEC xp_instance_regread
    N'HKEY_LOCAL_MACHINE',
    N'Software\Microsoft\MSSQLServer\MSSQLServer',
    N'BackupDirectory';";

                string masterConn = MasterConnectionString();
                using var conn = new SqlConnection(masterConn);
                conn.Open();
                using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 10 };
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    string? path = reader["Data"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(path))
                        return path;
                }
            }
            catch { /* fallback below */ }

            // Запасной вариант — папка «SQL\Backup» рядом с exe
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SQL", "Backup");
        }

        // ── BACKUP ──────────────────────────────────────────────────────────

        private void BrowseBackupFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Title            = "Выберите папку и имя файла резервной копии",
                Filter           = "SQL Server Backup (*.bak)|*.bak",
                FileName         = $"{DatabaseName}_{DateTime.Now:yyyy-MM-dd_HH-mm}.bak",
                InitialDirectory = tbBackupFolder.Text
            };
            if (dlg.ShowDialog() == true)
                tbBackupFolder.Text = Path.GetDirectoryName(dlg.FileName)!;
        }

        private async void Backup_Click(object sender, RoutedEventArgs e)
        {
            string folder = tbBackupFolder.Text.Trim();
            if (string.IsNullOrEmpty(folder))
            {
                ShowStatus(tbBackupStatus, "❌ Укажите папку для сохранения.", isError: true);
                return;
            }

            // Создаём папку от имени приложения (не SQL Server)
            try { Directory.CreateDirectory(folder); }
            catch
            {
                ShowStatus(tbBackupStatus,
                    "❌ Не удалось создать папку. Попробуйте выбрать папку, доступную SQL Server " +
                    "(например C:\\SQLBackup).", isError: true);
                return;
            }

            // Проверяем, что SQL Server сможет писать в эту папку,
            // запросив его стандартный бэкап-каталог и сравнив корни.
            string fileName   = $"{DatabaseName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.bak";
            string backupPath = Path.Combine(folder, fileName);

            SetBusy(true, isBackup: true);
            ShowStatus(tbBackupStatus, "⏳ Выполняется резервное копирование…", isError: false);

            // SQL Server сам открывает файл — путь должен быть доступен
            // учётной записи службы (NT Service\MSSQL$SQLEXPRESS или NETWORK SERVICE).
            // Используем xp_create_subdir чтобы SQL Server сам создал папку
            // под своей учёткой, после чего у него точно есть доступ.
            string mkdirSql   = $"EXEC xp_create_subdir N'{folder.Replace("'", "''")}';";
            string backupSql  = $@"
BACKUP DATABASE [{DatabaseName}]
TO DISK = N'{backupPath.Replace("'", "''")}'
WITH FORMAT, INIT,
     NAME = N'{DatabaseName} Backup {DateTime.Now:yyyy-MM-dd HH:mm}',
     STATS = 10, CHECKSUM;";

            try
            {
                await Task.Run(() =>
                {
                    // Просим SQL Server создать папку своей учёткой
                    try { ExecuteSql(mkdirSql); } catch { /* папка уже есть — ок */ }
                    ExecuteSql(backupSql);
                });

                ShowStatus(tbBackupStatus,
                    $"✅ Резервная копия создана:\n{backupPath}", isError: false);
            }
            catch (Exception ex)
            {
                // Если всё ещё ошибка доступа — даём понятную подсказку
                string hint = ex.Message.Contains("5(") || ex.Message.Contains("Access")
                    ? "\n\n💡 Совет: выберите папку, доступную SQL Server, например C:\\SQLBackup\\ " +
                      "или стандартную папку бэкапов SQL Server."
                    : string.Empty;

                ShowStatus(tbBackupStatus, $"❌ Ошибка: {ex.Message}{hint}", isError: true);
            }
            finally
            {
                SetBusy(false, isBackup: true);
            }
        }

        // ── RESTORE ─────────────────────────────────────────────────────────

        private void BrowseRestoreFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title  = "Выберите файл резервной копии",
                Filter = "SQL Server Backup (*.bak)|*.bak|Все файлы (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                tbRestoreFile.Text   = dlg.FileName;
                btnRestore.IsEnabled = true;
            }
        }

        private async void Restore_Click(object sender, RoutedEventArgs e)
        {
            string bakFile = tbRestoreFile.Text.Trim();
            if (!File.Exists(bakFile))
            {
                ShowStatus(tbRestoreStatus, "❌ Файл не найден.", isError: true);
                return;
            }

            var confirm = MessageBox.Show(
                $"Восстановить базу данных «{DatabaseName}» из файла:\n{bakFile}\n\n" +
                "⚠  Все текущие данные будут ЗАМЕНЕНЫ.\n" +
                "Приложение закроется по завершении.\n\n" +
                "Продолжить?",
                "Подтверждение восстановления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            SetBusy(busy: true, isBackup: false);
            ShowStatus(tbRestoreStatus, "⏳ Восстановление базы данных…", isError: false);

            // Верификация целостности файла
            string verifySql  = $@"
RESTORE VERIFYONLY
FROM DISK = N'{bakFile.Replace("'", "''")}'
WITH CHECKSUM;";

            // Восстановление: переключаем в single-user, восстанавливаем, возвращаем в multi-user
            string restoreSql = $@"
USE master;
ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [{DatabaseName}]
FROM DISK = N'{bakFile.Replace("'", "''")}'
WITH REPLACE, STATS = 10, RECOVERY;
ALTER DATABASE [{DatabaseName}] SET MULTI_USER;";

            try
            {
                await Task.Run(() =>
                {
                    ExecuteSql(verifySql);
                    ExecuteSql(restoreSql);
                });

                MessageBox.Show(
                    "✅ База данных успешно восстановлена.\nПриложение будет перезапущено.",
                    "Готово", MessageBoxButton.OK, MessageBoxImage.Information);

                System.Diagnostics.Process.Start(
                    System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                string hint = ex.Message.Contains("5(") || ex.Message.Contains("Access")
                    ? "\n\n💡 Убедитесь, что файл .bak находится в папке, доступной SQL Server."
                    : string.Empty;

                ShowStatus(tbRestoreStatus,
                    $"❌ Ошибка восстановления:\n{ex.Message}{hint}", isError: true);
                SetBusy(false, isBackup: false);
            }
        }

        // ── Вспомогательные методы ──────────────────────────────────────────

        private static string MasterConnectionString()
        {
            string db = DatabaseName;
            return ConnectionString
                .Replace($"Database={db}", "Database=master")
                .Replace($"Initial Catalog={db}", "Initial Catalog=master");
        }

        private static void ExecuteSql(string sql)
        {
            using var conn = new SqlConnection(MasterConnectionString());
            conn.Open();
            using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 300 };
            cmd.ExecuteNonQuery();
        }

        private void ShowStatus(System.Windows.Controls.TextBlock tb, string message, bool isError)
        {
            Dispatcher.Invoke(() =>
            {
                tb.Text       = message;
                tb.Foreground = isError
                    ? (System.Windows.Media.Brush)FindResource("DangerBrush")
                    : System.Windows.Media.Brushes.ForestGreen;
                tb.Visibility = Visibility.Visible;
            });
        }

        private void SetBusy(bool busy, bool isBackup)
        {
            Dispatcher.Invoke(() =>
            {
                if (isBackup)
                {
                    btnBackup.IsEnabled = !busy;
                    pbBackup.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
                    pbBackup.Value      = busy ? 0 : 100;
                }
                else
                {
                    btnRestore.IsEnabled = !busy;
                    pbRestore.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
                }
            });
        }

        private static string ExtractValue(string connStr, string key)
        {
            foreach (var part in connStr.Split(';'))
            {
                var kv = part.Split('=');
                if (kv.Length == 2 &&
                    kv[0].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                    return kv[1].Trim();
            }
            return key == "Database" ? "School_event" : ".\\SQLEXPRESS";
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
