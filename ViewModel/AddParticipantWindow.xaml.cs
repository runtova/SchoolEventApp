using System;
using System.Text.RegularExpressions;
using System.Windows;
using Microsoft.Data.SqlClient;

namespace SchoolEventApp
{
    public partial class AddParticipantWindow : Window
    {
        private readonly bool _isEdit;
        private readonly short _editCod;

        public AddParticipantWindow()
        {
            InitializeComponent();
            _isEdit = false;
        }

        public AddParticipantWindow(short editCod)
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
                tbTitle.Text = Title = "Редактирование участника";
                panelCod.Visibility = Visibility.Visible;

                try
                {
                    var dt = DatabaseHelper.ExecProc("sp_Participant_GetAll");
                    foreach (System.Data.DataRow r in dt.Rows)
                    {
                        if (Convert.ToInt16(r["student_cod"]) != _editCod)
                            continue;

                        tbCod.Text = _editCod.ToString();
                        tbFIO.Text = r["student_FIO"].ToString();
                        tbClass.Text = r["class"].ToString();
                        tbEmail.Text = r["email_s"].ToString();
                        tbPhone.Text = r["phone_number_s"].ToString();
                        break;
                    }
                }
                catch (Exception)
                {
                    ShowError("Ошибка загрузки данных.");
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
                ShowError("Введите ФИО участника.");
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

            // 2. Проверка класса
            string className = tbClass.Text.Trim();
            if (string.IsNullOrWhiteSpace(className))
            {
                ShowError("Введите класс участника.");
                return;
            }
            if (className.Length > 10)
            {
                ShowError("Название класса слишком длинное (макс. 10 символов).");
                return;
            }
            // Проверка формата класса (например 8Б, 9А, 7Б)
            var classRegex = new Regex(@"^\d{1,2}[А-Я]$");
            if (!classRegex.IsMatch(className))
            {
                ShowError("Введите класс в формате: цифры + буква (например: 8Б, 9А, 11В)");
                return;
            }

            // 3. Проверка Email
            string email = tbEmail.Text.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                ShowError("Введите email участника.");
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
                ShowError("Введите номер телефона участника.");
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
                    DatabaseHelper.ExecProcNonQuery("sp_Participant_Update", new[]
                    {
                        new SqlParameter("@student_cod", _editCod),
                        new SqlParameter("@student_FIO", fio),
                        new SqlParameter("@class", className),
                        new SqlParameter("@email_s", email),
                        new SqlParameter("@phone_number_s", phone)
                    });
                }
                else
                {
                    // Проверка на дубликат email
                    if (IsEmailExists(email))
                    {
                        ShowError("Участник с таким email уже существует.");
                        return;
                    }

                    DatabaseHelper.ExecProcNonQuery("sp_Participant_Insert", new[]
                    {
                        new SqlParameter("@student_FIO", fio),
                        new SqlParameter("@class", className),
                        new SqlParameter("@email_s", email),
                        new SqlParameter("@phone_number_s", phone)
                    });
                }
                DialogResult = true;
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("UNIQUE") || ex.Message.Contains("duplicate"))
                    ShowError("Участник с таким email уже существует.");
                else if (ex.Message.Contains("String or binary data would be truncated"))
                    ShowError("Некоторые данные слишком длинные.\nПроверьте заполнение полей.");
                else
                    ShowError("Ошибка сохранения: " + ex.Message);
            }
            catch (Exception ex)
            {
                ShowError("Произошла ошибка: " + ex.Message);
            }
        }

        // Валидация email
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
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
            var digits = Regex.Replace(phone, @"[^\d]", "");
            return digits.Length == 9;
        }

        // Проверка существования email
        private bool IsEmailExists(string email)
        {
            try
            {
                var dt = DatabaseHelper.ExecProc("sp_Participant_GetAll");
                foreach (System.Data.DataRow row in dt.Rows)
                {
                    if (row["email_s"]?.ToString()?.ToLower() == email.ToLower())
                        return true;
                }
            }
            catch { }
            return false;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            tbFIO.Text = "";
            tbClass.Text = "";
            tbEmail.Text = "";
            tbPhone.Text = "";
            HideError();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ShowError(string msg)
        {
            tbError.Text = msg;
            borderError.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            borderError.Visibility = Visibility.Collapsed;
        }
    }
}