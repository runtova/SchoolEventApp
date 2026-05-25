using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Data.SqlClient;

namespace SchoolEventApp
{
    public partial class MainWindow : Window
    {
        // Debounce — запрос уходит через 350 мс после последнего символа
        private readonly SearchDebounce _debounceOrg;
        private readonly SearchDebounce _debounceParticipant;
        private readonly SearchDebounce _debounceEvent;
        private readonly SearchDebounce _debounceParticipation;
        private readonly SearchDebounce _debounceArchive;

        // Фильтры вкладки «Мероприятия»
        private string    _eventTypeFilter = "";
        private DateTime? _eventDateFrom   = null;
        private DateTime? _eventDateTo     = null;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;

            _debounceOrg           = new SearchDebounce(350, () => LoadOrganizers(tbSearchOrganizer.Text.Trim()));
            _debounceParticipant   = new SearchDebounce(350, () => LoadParticipants(tbSearchParticipant.Text.Trim()));
            _debounceEvent         = new SearchDebounce(350, () => LoadEvents(tbSearchEvent.Text.Trim()));
            _debounceParticipation = new SearchDebounce(350, () => LoadParticipation(tbSearchParticipation.Text.Trim()));
            _debounceArchive       = new SearchDebounce(350, () => LoadArchive(tbSearchArchive.Text.Trim()));
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            tbCurrentUser.Text = $"👤 {LoginWindow.CurrentFullName}  ({LoginWindow.CurrentRole})";

            // Показываем раздел «Администрирование» только администратору
            bool isAdmin = string.Equals(LoginWindow.CurrentRole, "admin",
                               StringComparison.OrdinalIgnoreCase);
            spAdminSection.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

            if (DatabaseHelper.TestConnection())
            {
                tbConnectionStatus.Text = "✔ Подключено к базе данных";
                LoadAllTables();
            }
            else
            {
                tbConnectionStatus.Text       = "✖ Нет подключения";
                tbConnectionStatus.Foreground = System.Windows.Media.Brushes.OrangeRed;
            }
        }

        // ── Загрузка таблиц ─────────────────────────────────────────────────

        public void LoadAllTables()
        {
            LoadOrganizers();
            LoadParticipants();
            LoadEvents();
            LoadParticipation();
            LoadArchive();
        }

        private void LoadOrganizers(string search = "")
        {
            var dt = string.IsNullOrEmpty(search)
                ? DatabaseHelper.ExecProc("sp_Organizer_GetAll")
                : DatabaseHelper.ExecProc("sp_Organizer_Search",
                    new[] { new SqlParameter("@SearchText", search) });
            dgOrganizer.ItemsSource = ToRowList(dt);
        }

        private void LoadParticipants(string search = "")
        {
            var dt = string.IsNullOrEmpty(search)
                ? DatabaseHelper.ExecProc("sp_Participant_GetAll")
                : DatabaseHelper.ExecProc("sp_Participant_Search",
                    new[] { new SqlParameter("@SearchText", search) });
            dgParticipant.ItemsSource = ToRowList(dt);
        }

        /// <summary>
        /// Начало текущего учебного года: 1 сентября.
        /// До сентября — берём прошлый год, с сентября — текущий.
        /// </summary>
        private static DateTime GetSchoolYearStart()
        {
            var today = DateTime.Today;
            int year = today.Month >= 9 ? today.Year : today.Year - 1;
            return new DateTime(year, 9, 1);
        }

        private void LoadEvents(string search = "")
        {
            var dt = string.IsNullOrEmpty(search)
                ? DatabaseHelper.ExecProc("sp_Event_GetAll")
                : DatabaseHelper.ExecProc("sp_Event_Search",
                    new[] { new SqlParameter("@SearchText", search) });

            var schoolYearStart = GetSchoolYearStart();
            var list = new List<dynamic>();
            int i = 1;
            foreach (DataRow row in dt.Rows)
            {
                if (!(row["date_e"] is DateTime d && d.Date >= schoolYearStart)) continue;

                // Фильтр по типу
                if (!string.IsNullOrEmpty(_eventTypeFilter))
                {
                    string t = row["type_e"]?.ToString()?.ToLower() ?? "";
                    if (!t.Contains(_eventTypeFilter)) continue;
                }

                // Фильтр по дате
                if (_eventDateFrom.HasValue && d.Date < _eventDateFrom.Value.Date) continue;
                if (_eventDateTo.HasValue   && d.Date > _eventDateTo.Value.Date)   continue;

                var item = new ExpandoObject() as IDictionary<string, object>;
                item["RowNumber"] = i++;
                foreach (DataColumn col in dt.Columns)
                    item[col.ColumnName] = row[col];

                if (dt.Columns.Contains("max_participants") &&
                    row["max_participants"] != DBNull.Value)
                {
                    int mp = Convert.ToInt32(row["max_participants"]);
                    item["max_participants"] = mp > 0 ? mp.ToString() : "—";
                }
                else
                {
                    item["max_participants"] = "—";
                }

                list.Add((ExpandoObject)item);
            }
            dgEvent.ItemsSource = list;
        }

        private void LoadArchive(string search = "")
        {
            // sp_Event_GetAll / sp_Event_Search уже содержат organizer_FIO
            // и participants_count — дополнительные запросы не нужны
            var dt = string.IsNullOrEmpty(search)
                ? DatabaseHelper.ExecProc("sp_Event_GetAll")
                : DatabaseHelper.ExecProc("sp_Event_Search",
                    new[] { new SqlParameter("@SearchText", search) });

            var schoolYearStart = GetSchoolYearStart();
            var list = new List<dynamic>();
            int i = 1;
            foreach (DataRow row in dt.Rows)
            {
                if (!(row["date_e"] is DateTime d && d.Date < schoolYearStart)) continue;

                var item = new ExpandoObject() as IDictionary<string, object>;
                item["RowNumber"] = i++;
                foreach (DataColumn col in dt.Columns)
                    item[col.ColumnName] = row[col];
                list.Add((ExpandoObject)item);
            }
            dgArchive.ItemsSource = list;
            if (tbArchiveCount != null)
                tbArchiveCount.Text =
                    $"Прошлые учебные годы (до {schoolYearStart:dd.MM.yyyy}): {list.Count} мероприятий";
        }

        private void LoadParticipation(string search = "")
        {
            var dt = string.IsNullOrEmpty(search)
                ? DatabaseHelper.ExecProc("sp_Participation_GetAll")
                : DatabaseHelper.ExecProc("sp_Participation_Search",
                    new[] { new SqlParameter("@SearchText", search) });

            // Нумеруем строки внутри каждой группы
            var rows = new List<dynamic>();
            var groupCounters = new Dictionary<string, int>();
            foreach (DataRow row in dt.Rows)
            {
                var item = new ExpandoObject() as IDictionary<string, object>;
                foreach (DataColumn col in dt.Columns)
                    item[col.ColumnName] = row[col];

                string eventName = row["event_name"]?.ToString() ?? "";
                if (!groupCounters.ContainsKey(eventName)) groupCounters[eventName] = 0;
                item["RowNumber"] = ++groupCounters[eventName];
                rows.Add((ExpandoObject)item);
            }

            // Группировка по названию мероприятия
            var view = new System.Windows.Data.CollectionViewSource { Source = rows };
            view.GroupDescriptions.Add(
                new System.Windows.Data.PropertyGroupDescription("event_name"));
            dgParticipation.ItemsSource = view.View;
        }

        // ── Вспомогательный метод: DataTable → List<dynamic> ───────────────

        private List<dynamic> ToRowList(DataTable dt)
        {
            var list = new List<dynamic>();
            int i = 1;
            foreach (DataRow row in dt.Rows)
            {
                var item = new ExpandoObject() as IDictionary<string, object>;
                item["RowNumber"] = i++;
                foreach (DataColumn col in dt.Columns)
                    item[col.ColumnName] = row[col];
                list.Add((ExpandoObject)item);
            }
            return list;
        }

        // ── Живой поиск через debounce ───────────────────────────────────────

        private void tbSearchOrganizer_TextChanged(object sender, TextChangedEventArgs e)
            => _debounceOrg.Trigger();

        private void tbSearchParticipant_TextChanged(object sender, TextChangedEventArgs e)
            => _debounceParticipant.Trigger();

        private void tbSearchEvent_TextChanged(object sender, TextChangedEventArgs e)
            => _debounceEvent.Trigger();

        private void tbSearchParticipation_TextChanged(object sender, TextChangedEventArgs e)
            => _debounceParticipation.Trigger();

        // ── Сброс фильтров ──────────────────────────────────────────────────

        private void ClearOrganizersFilter_Click(object sender, RoutedEventArgs e)
            => tbSearchOrganizer.Text = "";

        private void ClearParticipantsFilter_Click(object sender, RoutedEventArgs e)
            => tbSearchParticipant.Text = "";

        private void EvChip_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Primitives.ToggleButton clicked) return;
            foreach (var chip in new[] { evChipAll, evChipEdu, evChipSport, evChipCult, evChipSoc })
                chip.IsChecked = chip == clicked;
            _eventTypeFilter = clicked.Tag?.ToString() ?? "";
            LoadEvents(tbSearchEvent.Text.Trim());
        }

        private void EventDateFilter_Changed(object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _eventDateFrom = dpEventFrom.SelectedDate;
            _eventDateTo   = dpEventTo.SelectedDate;
            LoadEvents(tbSearchEvent.Text.Trim());
        }

        private void ClearDateFilter_Click(object sender, RoutedEventArgs e)
        {
            dpEventFrom.SelectedDate = null;
            dpEventTo.SelectedDate   = null;
            _eventDateFrom = null;
            _eventDateTo   = null;
            LoadEvents(tbSearchEvent.Text.Trim());
        }

        private void ClearEventsFilter_Click(object sender, RoutedEventArgs e)
        {
            tbSearchEvent.Text       = "";
            dpEventFrom.SelectedDate = null;
            dpEventTo.SelectedDate   = null;
            _eventDateFrom           = null;
            _eventDateTo             = null;
            _eventTypeFilter         = "";
            foreach (var chip in new[] { evChipAll, evChipEdu, evChipSport, evChipCult, evChipSoc })
                chip.IsChecked = chip == evChipAll;
            LoadEvents();
        }

        private void ClearParticipationFilter_Click(object sender, RoutedEventArgs e)
            => tbSearchParticipation.Text = "";

        // ── Добавление ──────────────────────────────────────────────────────

        private void AddOrganizer_Click(object sender, RoutedEventArgs e)
        {
            if (new AddOrganizerWindow { Owner = this }.ShowDialog() == true)
                LoadOrganizers();
        }

        private void AddParticipant_Click(object sender, RoutedEventArgs e)
        {
            if (new AddParticipantWindow { Owner = this }.ShowDialog() == true)
                LoadParticipants();
        }

        private void AddEvent_Click(object sender, RoutedEventArgs e)
        {
            if (new AddEventWindow { Owner = this }.ShowDialog() == true)
                LoadEvents();
        }

        private void AddParticipation_Click(object sender, RoutedEventArgs e)
        {
            if (new AddParticipationWindow { Owner = this }.ShowDialog() == true)
                LoadParticipation();
        }

        // ── Редактирование ──────────────────────────────────────────────────

        private void EditOrganizer_Click(object sender, RoutedEventArgs e)
        {
            if (dgOrganizer.SelectedItem is not IDictionary<string, object> row) return;
            if (new AddOrganizerWindow(Convert.ToInt16(row["organizer_cod"])) { Owner = this }.ShowDialog() == true)
                LoadOrganizers();
        }

        private void EditParticipant_Click(object sender, RoutedEventArgs e)
        {
            if (dgParticipant.SelectedItem is not IDictionary<string, object> row) return;
            if (new AddParticipantWindow(Convert.ToInt16(row["student_cod"])) { Owner = this }.ShowDialog() == true)
                LoadParticipants();
        }

        private void EditEvent_Click(object sender, RoutedEventArgs e)
        {
            if (dgEvent.SelectedItem is not IDictionary<string, object> row) return;
            if (new AddEventWindow(Convert.ToInt16(row["event_cod"])) { Owner = this }.ShowDialog() == true)
                LoadEvents();
        }

        // ── Двойной клик = редактирование ───────────────────────────────────

        private void dgOrganizer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgOrganizer.SelectedItem is not IDictionary<string, object> row) return;
            new AddOrganizerWindow(Convert.ToInt16(row["organizer_cod"])) { Owner = this }.ShowDialog();
            LoadOrganizers();
        }

        private void dgParticipant_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgParticipant.SelectedItem is not IDictionary<string, object> row) return;
            new AddParticipantWindow(Convert.ToInt16(row["student_cod"])) { Owner = this }.ShowDialog();
            LoadParticipants();
        }

        private void dgEvent_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgEvent.SelectedItem is not IDictionary<string, object> row) return;
            new AddEventWindow(Convert.ToInt16(row["event_cod"])) { Owner = this }.ShowDialog();
            LoadEvents();
        }

        // ── Удаление ────────────────────────────────────────────────────────

        private void DeleteSelectedOrganizer_Click(object sender, RoutedEventArgs e)
        {
            if (dgOrganizer.SelectedItem is not IDictionary<string, object> row) return;
            if (!Confirm($"Удалить организатора «{row["organizer_FIO"]}»?")) return;
            if (DatabaseHelper.ExecProcNonQuery("sp_Organizer_Delete",
                new[] { new SqlParameter("@organizer_cod", Convert.ToInt16(row["organizer_cod"])) }))
                LoadOrganizers();
        }

        private void DeleteSelectedParticipant_Click(object sender, RoutedEventArgs e)
        {
            if (dgParticipant.SelectedItem is not IDictionary<string, object> row) return;
            if (!Confirm($"Удалить участника «{row["student_FIO"]}»?")) return;
            if (DatabaseHelper.ExecProcNonQuery("sp_Participant_Delete",
                new[] { new SqlParameter("@student_cod", Convert.ToInt16(row["student_cod"])) }))
                LoadParticipants();
        }

        private void DeleteSelectedEvent_Click(object sender, RoutedEventArgs e)
        {
            if (dgEvent.SelectedItem is not IDictionary<string, object> row) return;
            short evCod = Convert.ToInt16(row["event_cod"]);

            // participants_count уже есть в строке — приходит из sp_Event_GetAll через JOIN
            int participantsCount = row.ContainsKey("participants_count")
                ? Convert.ToInt32(row["participants_count"])
                : 0;

            if (participantsCount > 0)
            {
                MessageBox.Show(
                    $"⛔  Вы не можете удалить мероприятие\n" +
                    $"«{row["event_name"]}»\n\n" +
                    $"На него уже записано участников: {participantsCount}.\n" +
                    "Сначала удалите все записи участников, а затем повторите попытку.",
                    "Удаление невозможно",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!Confirm($"Удалить мероприятие «{row["event_name"]}»?")) return;
            if (DatabaseHelper.ExecProcNonQuery("sp_Event_Delete",
                new[] { new SqlParameter("@event_cod", evCod) }))
                LoadEvents();
        }

        private void DeleteSelectedParticipation_Click(object sender, RoutedEventArgs e)
        {
            if (dgParticipation.SelectedItem is not IDictionary<string, object> row) return;
            if (!Confirm($"Удалить участие «{row["student_FIO"]}» в мероприятии «{row["event_name"]}»?")) return;
            if (DatabaseHelper.ExecProcNonQuery("sp_Participation_Delete", new[]
            {
                new SqlParameter("@event_cod",   Convert.ToInt16(row["event_cod"])),
                new SqlParameter("@student_cod", Convert.ToInt16(row["student_cod"]))
            }))
                LoadParticipation();
        }

        // ── Прочие кнопки ───────────────────────────────────────────────────

        private void tbSearchArchive_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
            => _debounceArchive.Trigger();

        private void ClearArchiveFilter_Click(object sender, RoutedEventArgs e)
            => tbSearchArchive.Text = "";

        private void OpenReports_Click(object sender, RoutedEventArgs e)
            => new ReportsWindow { Owner = this }.ShowDialog();

        private void OpenDashboard_Click(object sender, RoutedEventArgs e)
            => new DashboardWindow { Owner = this }.ShowDialog();

        private void OpenBackup_Click(object sender, RoutedEventArgs e)
        {
            SetActiveNav(btnNavBackup);
            new BackupWindow { Owner = this }.ShowDialog();
            // После закрытия окна снимаем выделение с кнопки
            SetActiveNav(null);
        }

        // ── Боковая навигация ────────────────────────────────────────────────

        private void SetActiveNav(System.Windows.Controls.Button active)
        {
            var navButtons = new[]
            {
                btnNavOrganizers, btnNavParticipants, btnNavEvents,
                btnNavParticipation, btnNavArchive, btnNavDashboard, btnNavReports, btnNavBackup
            };
            foreach (var btn in navButtons)
            {
                if (btn == null) continue;
                btn.Style = (System.Windows.Style)FindResource(
                    btn == active ? "NavButtonActiveStyle" : "NavButtonStyle");
            }
        }

        private void NavOrganizers_Click(object sender, RoutedEventArgs e)
        {
            tcMain.SelectedItem = tabOrganizers;
            tbPageTitle.Text = "Организаторы";
            SetActiveNav(btnNavOrganizers);
        }

        private void NavParticipants_Click(object sender, RoutedEventArgs e)
        {
            tcMain.SelectedItem = tabParticipants;
            tbPageTitle.Text = "Участники";
            SetActiveNav(btnNavParticipants);
        }

        private void NavEvents_Click(object sender, RoutedEventArgs e)
        {
            tcMain.SelectedItem = tabEvents;
            tbPageTitle.Text = "Мероприятия";
            SetActiveNav(btnNavEvents);
        }

        private void NavParticipation_Click(object sender, RoutedEventArgs e)
        {
            tcMain.SelectedItem = tabParticipation;
            tbPageTitle.Text = "Участие";
            SetActiveNav(btnNavParticipation);
        }

        private void NavArchive_Click(object sender, RoutedEventArgs e)
        {
            tcMain.SelectedItem = tabArchive;
            tbPageTitle.Text = "Архив мероприятий";
            SetActiveNav(btnNavArchive);
        }

        private void RefreshAll_Click(object sender, RoutedEventArgs e)
            => LoadAllTables();

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            if (!Confirm("Вы уверены, что хотите выйти из системы?")) return;
            new LoginWindow().Show();
            Close();
        }

        // Кнопки "Найти" — теперь просто форсируют debounce без ожидания
        private void SearchOrganizers_Click(object sender, RoutedEventArgs e)
            => LoadOrganizers(tbSearchOrganizer.Text.Trim());
        private void SearchParticipants_Click(object sender, RoutedEventArgs e)
            => LoadParticipants(tbSearchParticipant.Text.Trim());
        private void SearchEvents_Click(object sender, RoutedEventArgs e)
            => LoadEvents(tbSearchEvent.Text.Trim());
        private void SearchParticipation_Click(object sender, RoutedEventArgs e)
            => LoadParticipation(tbSearchParticipation.Text.Trim());

        // ── Утилита ─────────────────────────────────────────────────────────

        private static bool Confirm(string question) =>
            MessageBox.Show(question, "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }
}
