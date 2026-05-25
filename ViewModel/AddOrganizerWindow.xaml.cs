using System;
using System.Text.RegularExpressions;
using System.Windows;
using Microsoft.Data.SqlClient;

namespace SchoolEventApp
{
    public partial class AddOrganizerWindow : Window
    {
        private readonly bool _isEdit;
        private readonly short _editCod;

        public AddOrganizerWindow()
        {
            InitializeComponent();
            _isEdit = false;
        }

        public AddOrganizerWindow(short editCod)
        {
            InitializeComponent();
            _isEdit = true;
            _editCod = editCod;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_isEdit)
            {
                tbTitle.Text = Title = "Редактирование организатора";
                panelCod.Visibility = Visibility.Visible;
                try
                {
                    var dt = DatabaseHelper.ExecProc("sp_Organizer_GetAll");
                    foreach (System.Data.DataRow r in dt.Rows)
                    {
                        if (Convert.ToInt16(r["organizer_cod"]) != _editCod) continue;
                        tbCod.Text = _editCod.ToString();
                        tbFIO.Text = r["organizer_FIO"].ToString();
                        tbJob.Text = r["job_name"].ToString();
                        tbEmail.Text = r["email_0"].ToString();
                        tbPhone.Text = r["phone_number_o"].ToString();
                        break;
                    }
                }
                catch (Exception ex)
                {
                    ShowError("Ошибка загрузки данных: " + ex.Message);
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            HideError();

            // =============================================
            // СУПЕР-ВАЛИДАЦИЯ
            // =============================================

            // 1. Проверка ФИО
            string fio = tbFIO.Text.Trim();
            if (string.IsNullOrWhiteSpace(fio))
            {
                ShowError("Введите ФИО организатора.");
                return;
            }
            if (fio.Length < 5)
            {
                ShowError("ФИО должно содержать минимум 5 символов.");
                return;
            }
            if (fio.Length > 100)
            {
                ShowError("ФИО не должно превышать 100 символов.");
                return;
            }
            // Проверка, что ФИО содержит хотя бы два слова
            var fioParts = fio.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (fioParts.Length < 2)
            {
                ShowError("Введите полное ФИО (Фамилия и Имя обязательны).");
                return;
            }

            // 2. Проверка должности
            string job = tbJob.Text.Trim();
            if (string.IsNullOrWhiteSpace(job))
            {
                ShowError("Введите должность организатора.");
                return;
            }
            if (job.Length > 50)
            {
                ShowError("Должность не должна превышать 50 символов.");
                return;
            }

            // 3. Проверка Email
            string email = tbEmail.Text.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                ShowError("Введите email организатора.");
                return;
            }
            if (!IsValidEmail(email))
            {
                ShowError("Введите корректный email адрес.\nПример: name@gmail.com");
                return;
            }
            if (email.Length > 100)
            {
                ShowError("Email не должен превышать 100 символов.");
                return;
            }

            // 4. Проверка телефона
            string phone = tbPhone.Text.Trim();
            if (string.IsNullOrWhiteSpace(phone))
            {
                ShowError("Введите номер телефона организатора.");
                return;
            }
            if (!IsValidPhone(phone))
            {
                ShowError("Введите корректный номер телефона.\nФормат: 9 цифр (например 071234567)");
                return;
            }

            try
            {
                if (_isEdit)
                {
                    DatabaseHelper.ExecProcNonQuery("sp_Organizer_Update", new[]
                    {
                        new SqlParameter("@organizer_cod", _editCod),
                        new SqlParameter("@organizer_FIO", fio),
                        new SqlParameter("@job_name", job),
                        new SqlParameter("@email_0", email),
                        new SqlParameter("@phone_number_o", phone)
                    });
                }
                else
                {
                    // Проверка на дубликат email
                    if (IsEmailExists(email))
                    {
                        ShowError("Организатор с таким email уже существует.");
                        return;
                    }

                    DatabaseHelper.ExecProcNonQuery("sp_Organizer_Insert", new[]
                    {
                        new SqlParameter("@organizer_FIO", fio),
                        new SqlParameter("@job_name", job),
                        new SqlParameter("@email_0", email),
                        new SqlParameter("@phone_number_o", phone)
                    });
                }
                DialogResult = true;
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("UNIQUE") || ex.Message.Contains("duplicate"))
                    ShowError("Организатор с таким email уже существует.");
                else
                    ShowError("Ошибка сохранения: " + ex.Message);
            }
            catch (Exception ex)
            {
                ShowError("Ошибка сохранения: " + ex.Message);
            }
        }

        // Валидация email
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                // Стандартный regex для email
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        // Валидация телефона (9 цифр)
        private bool IsValidPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            // Удаляем все нецифровые символы
            var digits = Regex.Replace(phone, @"[^\d]", "");
            // Проверяем, что ровно 9 цифр
            return digits.Length == 9;
        }

        // Проверка существования email
        private bool IsEmailExists(string email)
        {
            try
            {
                var dt = DatabaseHelper.ExecProc("sp_Organizer_GetAll");
                foreach (System.Data.DataRow row in dt.Rows)
                {
                    if (row["email_0"]?.ToString()?.ToLower() == email.ToLower())
                        return true;
                }
            }
            catch { }
            return false;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            tbFIO.Text = "";
            tbJob.Text = "";
            tbEmail.Text = "";
            tbPhone.Text = "";
            HideError();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;

        private void ShowError(string msg)
        {
            tbError.Text = msg;
            borderError.Visibility = Visibility.Visible;
        }

        private void HideError()
            => borderError.Visibility = Visibility.Collapsed;
    }
}