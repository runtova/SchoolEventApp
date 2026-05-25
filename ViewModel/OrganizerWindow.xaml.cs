using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Windows;
using System.Windows.Controls;

namespace SchoolEventApp
{
    public partial class OrganizerWindow : Window
    {
        private readonly string _myFIO      = LoginWindow.CurrentFullName;
        private short           _myOrgCod   = 0;   // код организатора текущего пользователя

        private readonly SearchDebounce _debounce;
        private readonly SearchDebounce _debounceArchive;

        public OrganizerWindow()
        {
            InitializeComponent();
            _debounce        = new SearchDebounce(350, () => LoadEvents(tbSearch.Text.Trim()));
            _debounceArchive = new SearchDebounce(350, () => LoadArchive(tbSearchArchive.Text.Trim()));
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            tbCurrentUser.Text = $"👤 {_myFIO}  (организатор)";

            // Получаем код организатора по username текущего пользователя
            _myOrgCod = ResolveOrganizerCod();

            if (_myOrgCod == 0)
            {
                // Если не нашли — предупреждаем, но всё равно показываем окно
                MessageBox.Show(
                    "Не удалось определить ваш код организатора.\n" +
                    "Убедитесь, что email учётной записи совпадает с email в таблице Organizer.",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            LoadEvents();
            LoadArchive();
        }

        // ── Определение кода организатора ────────────────────────────────────

        /// <summary>
        /// Ищет запись в таблице Organizer, чей email совпадает с email пользователя
        /// из таблицы Users (по текущему username). Возвращает organizer_cod или 0.
        /// </summary>
        private short ResolveOrganizerCod()
        {
            try
            {
                var dt = DatabaseHelper.ExecProc("sp_Organizer_GetByUsername",
                    new[] { new SqlParameter("@username", LoginWindow.CurrentUser) });

                if (dt != null && dt.Rows.Count > 0)
                    return Convert.ToInt16(dt.Rows[0]["organizer_cod"]);
            }
            catch { /* оставляем 0, обработка ниже */ }
            return 0;
        }

        // ── Загрузка мероприятий ──────────────────────────────────────────────

        private static DateTime GetSchoolYearStart()
        {
            var today = DateTime.Today;
            int year  = today.Month >= 9 ? today.Year : today.Year - 1;
            return new DateTime(year, 9, 1);
        }

        private void LoadEvents(string search = "")
        {
            DataTable dt;
            if (string.IsNullOrEmpty(search))
                dt = DatabaseHelper.ExecProc("sp_Event_GetByOrganizerFIO",
                    new[] { new SqlParameter("@OrganizerFIO", _myFIO) });
            else
                dt = DatabaseHelper.ExecProc("sp_Event_SearchByOrganizerFIO",
                    new[]
                    {
                        new SqlParameter("@OrganizerFIO", _myFIO),
                        new SqlParameter("@SearchText",   search)
                    });

            var partCount       = GetParticipantCounts();
            var schoolYearStart = GetSchoolYearStart();
            var list            = new List<dynamic>();
            int i = 1;
            foreach (DataRow row in dt.Rows)
            {
                if (!(row["date_e"] is DateTime d && d.Date >= schoolYearStart)) continue;
                var item = new ExpandoObject() as IDictionary<string, object>;
                item["RowNumber"] = i++;
                foreach (DataColumn col in dt.Columns)
                    item[col.ColumnName] = row[col];
                short evCod = Convert.ToInt16(row["event_cod"]);
                item["participants_count"] = partCount.ContainsKey(evCod) ? partCount[evCod] : 0;
                list.Add((ExpandoObject)item);
            }

            dgEvents.ItemsSource = list;
            var sy = GetSchoolYearStart();
            tbCount.Text = $"Учебный год {sy.Year}–{sy.Year + 1}: {list.Count} мероприятий";
        }

        private Dictionary<short, int> GetParticipantCounts()
        {
            var map = new Dictionary<short, int>();
            var dt  = DatabaseHelper.ExecProc("sp_Participation_GetAll");
            foreach (DataRow row in dt.Rows)
            {
                short evCod = Convert.ToInt16(row["event_cod"]);
                map[evCod] = map.ContainsKey(evCod) ? map[evCod] + 1 : 1;
            }
            return map;
        }

        // ── Архив прошедших мероприятий ───────────────────────────────────────

        private void LoadArchive(string search = "")
        {
            var schoolYearStart = GetSchoolYearStart();
            DataTable dt;
            if (string.IsNullOrEmpty(search))
                dt = DatabaseHelper.ExecProc("sp_Event_GetByOrganizerFIO",
                    new[] { new SqlParameter("@OrganizerFIO", _myFIO) });
            else
                dt = DatabaseHelper.ExecProc("sp_Event_SearchByOrganizerFIO",
                    new[]
                    {
                        new SqlParameter("@OrganizerFIO", _myFIO),
                        new SqlParameter("@SearchText",   search)
                    });

            var partCount = GetParticipantCounts();
            var list      = new List<dynamic>();
            int i = 1;
            foreach (DataRow row in dt.Rows)
            {
                if (!(row["date_e"] is DateTime d && d.Date < schoolYearStart)) continue;
                var item = new ExpandoObject() as IDictionary<string, object>;
                item["RowNumber"] = i++;
                foreach (DataColumn col in dt.Columns)
                    item[col.ColumnName] = row[col];
                short evCod = Convert.ToInt16(row["event_cod"]);
                item["participants_count"] = partCount.ContainsKey(evCod) ? partCount[evCod] : 0;
                list.Add((ExpandoObject)item);
            }
            if (dgArchive != null) dgArchive.ItemsSource = list;
            if (tbArchiveCount != null)
            {
                var sy = GetSchoolYearStart();
                tbArchiveCount.Text = $"Прошлые годы (до {sy:dd.MM.yyyy}): {list.Count} мероприятий";
            }
        }

        // ── Поиск ─────────────────────────────────────────────────────────────

        private void tbSearch_TextChanged(object sender, TextChangedEventArgs e)
            => _debounce.Trigger();

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            tbSearch.Text = "";
            LoadEvents();
        }

        private void tbSearchArchive_TextChanged(object sender, TextChangedEventArgs e)
            => _debounceArchive.Trigger();

        private void ClearArchiveFilter_Click(object sender, RoutedEventArgs e)
        {
            tbSearchArchive.Text = "";
            LoadArchive();
        }

        // ── Добавить мероприятие ──────────────────────────────────────────────

        private void AddEvent_Click(object sender, RoutedEventArgs e)
        {
            if (_myOrgCod == 0)
            {
                MessageBox.Show(
                    "Невозможно добавить мероприятие: не найден ваш код организатора.\n" +
                    "Обратитесь к администратору.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Передаём hideCode=true и зафиксированный код организатора
            var win = new AddEventWindow(hideCode: true, fixedOrganizerCod: _myOrgCod)
            { Owner = this };
            if (win.ShowDialog() == true)
                LoadEvents(tbSearch.Text.Trim());
        }

        // ── Редактировать мероприятие ─────────────────────────────────────────

        private void EditEvent_Click(object sender, RoutedEventArgs e)
        {
            if (dgEvents.SelectedItem is not IDictionary<string, object> row)
            {
                MessageBox.Show("Выберите мероприятие для редактирования.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_myOrgCod == 0)
            {
                MessageBox.Show("Невозможно редактировать: не найден ваш код организатора.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            short evCod = Convert.ToInt16(row["event_cod"]);

            // Передаём зафиксированный код организатора
            var win = new AddEventWindow(evCod, hideCode: true, fixedOrganizerCod: _myOrgCod)
            { Owner = this };
            if (win.ShowDialog() == true)
                LoadEvents(tbSearch.Text.Trim());
        }

        // ── Участники выбранного мероприятия ──────────────────────────────────

        private void ShowParticipants_Click(object sender, RoutedEventArgs e)
        {
            if (dgEvents.SelectedItem is not IDictionary<string, object> row)
            {
                MessageBox.Show("Выберите мероприятие.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            short  evCod  = Convert.ToInt16(row["event_cod"]);
            string evName = row["event_name"].ToString();
            new ParticipantsWindow(evCod, evName) { Owner = this }.ShowDialog();
        }

        // ── Обновить / Выход ──────────────────────────────────────────────────

        private void Refresh_Click(object sender, RoutedEventArgs e)
            => LoadEvents(tbSearch.Text.Trim());

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Выйти из системы?", "Выход",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            new LoginWindow().Show();
            Close();
        }

        // ── Переключение вкладок ──────────────────────────────────────────────

        private void TabUpcoming_Click(object sender, RoutedEventArgs e)
        {
            panelUpcoming.Visibility = Visibility.Visible;
            panelArchive.Visibility  = Visibility.Collapsed;

            btnTabUpcoming.BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(79, 93, 158));
            btnTabUpcoming.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(79, 93, 158));
            btnTabUpcoming.FontWeight = FontWeights.SemiBold;

            btnTabArchive.BorderBrush = System.Windows.Media.Brushes.Transparent;
            btnTabArchive.Foreground  = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(156, 163, 175));
            btnTabArchive.FontWeight = FontWeights.Normal;
        }

        private void TabArchive_Click(object sender, RoutedEventArgs e)
        {
            panelUpcoming.Visibility = Visibility.Collapsed;
            panelArchive.Visibility  = Visibility.Visible;

            btnTabArchive.BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(79, 93, 158));
            btnTabArchive.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(79, 93, 158));
            btnTabArchive.FontWeight = FontWeights.SemiBold;

            btnTabUpcoming.BorderBrush = System.Windows.Media.Brushes.Transparent;
            btnTabUpcoming.Foreground  = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(156, 163, 175));
            btnTabUpcoming.FontWeight = FontWeights.Normal;

            LoadArchive(tbSearchArchive.Text.Trim());
        }

        private void dgEvents_MouseDoubleClick(object sender,
            System.Windows.Input.MouseButtonEventArgs e)
            => EditEvent_Click(sender, e);
    }
}
