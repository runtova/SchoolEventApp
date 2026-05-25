using Microsoft.Data.SqlClient;
using System;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace SchoolEventApp
{
    public partial class LoginWindow : Window
    {
        public static string CurrentUser     { get; private set; }
        public static string CurrentFullName { get; private set; }
        public static string CurrentRole     { get; private set; }
        public static short  CurrentStudentCod { get; private set; } = -1; // ✅ ДОБАВЛЕНО

        private const int SaltSize   = 16;
        private const int HashSize   = 32;
        private const int Iterations = 100_000;

        public LoginWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => tbLoginUser.Focus();
            pbLoginPass.KeyDown += (s, e) =>
            { if (e.Key == System.Windows.Input.Key.Enter) Login_Click(s, e); };
        }

        // ── Переключение вкладок ─────────────────────────────────────────────

        private void TabLogin_Click(object sender, RoutedEventArgs e)
        {
            panelLogin.Visibility    = Visibility.Visible;
            panelRegister.Visibility = Visibility.Collapsed;
            SetTabActive(btnTabLogin, "#4F5D9E");
            SetTabInactive(btnTabRegister);
        }

        private void TabRegister_Click(object sender, RoutedEventArgs e)
        {
            panelLogin.Visibility    = Visibility.Collapsed;
            panelRegister.Visibility = Visibility.Visible;
            SetTabActive(btnTabRegister, "#4F5D9E");
            SetTabInactive(btnTabLogin);
        }

        private void SetTabActive(System.Windows.Controls.Button btn, string color)
        {
            var c = (System.Windows.Media.Color)
                System.Windows.Media.ColorConverter.ConvertFromString(color);
            btn.BorderBrush = new System.Windows.Media.SolidColorBrush(c);
            btn.Foreground  = new System.Windows.Media.SolidColorBrush(c);
            btn.FontWeight  = FontWeights.SemiBold;
        }

        private void SetTabInactive(System.Windows.Controls.Button btn)
        {
            btn.BorderBrush = System.Windows.Media.Brushes.Transparent;
            btn.Foreground  = System.Windows.Media.Brushes.DarkGray;
            btn.FontWeight  = FontWeights.Normal;
        }

        // ── Вход ─────────────────────────────────────────────────────────────

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            tbLoginError.Visibility = Visibility.Collapsed;

            string username = tbLoginUser.Text.Trim();
            string password = pbLoginPass.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            { ShowError(tbLoginError, "Заполните все поля."); return; }

            var dt = DatabaseHelper.ExecProc("sp_User_GetHash",
                new[] { new SqlParameter("@username", username) });

            if (dt == null || dt.Rows.Count == 0)
            { ShowError(tbLoginError, "Неверное имя пользователя или пароль."); return; }

            string storedHash = dt.Rows[0]["password_hash"].ToString();

            bool passwordOk = storedHash.Length == 64
                ? Hash_SHA256(password) == storedHash
                : VerifyPbkdf2(password, storedHash);

            if (!passwordOk)
            { ShowError(tbLoginError, "Неверное имя пользователя или пароль."); return; }

            var loginDt = DatabaseHelper.ExecProc("sp_User_Login",
                new[] { new SqlParameter("@username", username) });

            if (loginDt == null || loginDt.Rows.Count == 0)
            { ShowError(tbLoginError, "Ошибка при входе. Попробуйте ещё раз."); return; }

            CurrentUser     = loginDt.Rows[0]["username"].ToString();
            CurrentFullName = loginDt.Rows[0]["full_name"].ToString();
            CurrentRole     = loginDt.Rows[0]["role"].ToString();

            // ✅ Читаем student_cod из Users (теперь sp_User_Login возвращает его)
            CurrentStudentCod = loginDt.Rows[0]["student_cod"] != DBNull.Value
                ? Convert.ToInt16(loginDt.Rows[0]["student_cod"])
                : (short)-1;

            Window nextWindow = CurrentRole switch
            {
                "admin"     => new MainWindow(),
                "organizer" => new OrganizerWindow(),
                _           => new UserWindow()
            };

            nextWindow.Show();
            Close();
        }

        // ── Регистрация ──────────────────────────────────────────────────────

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            tbRegError.Visibility = Visibility.Collapsed;

            string username  = tbRegUser.Text.Trim();
            string fullName  = tbRegName.Text.Trim();
            string email     = tbRegEmail.Text.Trim();
            string password  = pbRegPass.Password;
            string password2 = pbRegPass2.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(fullName) ||
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(password2))
            { ShowError(tbRegError, "Заполните все поля."); return; }

            if (username.Length > 30)
            { ShowError(tbRegError, "Логин не должен превышать 30 символов."); return; }

            if (password != password2)
            { ShowError(tbRegError, "Пароли не совпадают."); return; }

            if (password.Length < 6)
            { ShowError(tbRegError, "Пароль должен быть не менее 6 символов."); return; }

            if (password.Length > 128)
            { ShowError(tbRegError, "Пароль не должен превышать 128 символов."); return; }

            if (!string.IsNullOrEmpty(email) && !IsValidEmail(email))
            { ShowError(tbRegError, "Введите корректный email-адрес."); return; }

            string passwordHash = HashPbkdf2(password);

            var dt = DatabaseHelper.ExecProc("sp_User_Register", new[]
            {
                new SqlParameter("@username",      username),
                new SqlParameter("@password_hash", passwordHash),
                new SqlParameter("@full_name",     fullName),
                new SqlParameter("@email",         string.IsNullOrEmpty(email)
                                                   ? (object)DBNull.Value : email)
            });

            if (dt != null && dt.Rows.Count > 0)
            {
                string role    = dt.Rows[0]["role"].ToString();
                string roleMsg = role == "organizer"
                    ? "Вы зарегистрированы как организатор."
                    : "Вы зарегистрированы как участник.";

                MessageBox.Show($"Регистрация прошла успешно!\n{roleMsg}\n\nТеперь войдите в систему.",
                    "Готово", MessageBoxButton.OK, MessageBoxImage.Information);

                TabLogin_Click(null, null);
                tbLoginUser.Text = username;
                pbLoginPass.Focus();
            }
        }

        // ── Хеширование PBKDF2 ───────────────────────────────────────────────

        public static string HashPbkdf2(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize);

            return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
        }

        public static bool VerifyPbkdf2(string password, string storedValue)
        {
            try
            {
                var parts = storedValue.Split(':');
                if (parts.Length != 2) return false;

                byte[] salt         = Convert.FromBase64String(parts[0]);
                byte[] storedHash   = Convert.FromBase64String(parts[1]);
                byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(
                    Encoding.UTF8.GetBytes(password),
                    salt,
                    Iterations,
                    HashAlgorithmName.SHA256,
                    HashSize);

                return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
            }
            catch { return false; }
        }

        public static string Hash_SHA256(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            var sb = new StringBuilder();
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        // ── Утилиты ──────────────────────────────────────────────────────────

        private static bool IsValidEmail(string email)
        {
            try { _ = new MailAddress(email); return true; }
            catch { return false; }
        }

        private static void ShowError(System.Windows.Controls.TextBlock tb, string msg)
        {
            tb.Text       = msg;
            tb.Visibility = Visibility.Visible;
        }
    }
}
