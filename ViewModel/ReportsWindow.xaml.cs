using Microsoft.Data.SqlClient;
using SchoolEventApp.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Windows;
using System.Windows.Controls;

namespace SchoolEventApp
{
    public partial class ReportsWindow : Window
    {
        // Общие фильтры для отчёта 2
        private DateTime? _dateFrom;
        private DateTime? _dateTo;

        public ReportsWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            FillSchoolYearCombo(cbYear1);
            FillSchoolYearCombo(cbYear2);
            FillSchoolYearCombo(cbYear3);

            LoadReport2Types();

            LoadReport1Preview();
            LoadReport2Preview();
            LoadReport3Preview();
        }

        // ── Учебные годы ─────────────────────────────────────────────────────

        private static int GetCurrentSchoolYear()
        {
            var today = DateTime.Today;
            return today.Month >= 9 ? today.Year : today.Year - 1;
        }

        private static string SchoolYearLabel(int y) => $"{y}/{y + 1}";

        private static void FillSchoolYearCombo(ComboBox cb)
        {
            var items = new List<string> { "(все годы)" };
            int cur = GetCurrentSchoolYear();
            for (int y = cur; y >= 2020; y--)
                items.Add(SchoolYearLabel(y));
            cb.ItemsSource   = items;
            cb.SelectedIndex = 1; // текущий учебный год по умолчанию
        }

        /// <summary>Возвращает (dateFrom, dateTo) по выбору в ComboBox учебного года.</summary>
        private static (DateTime? from, DateTime? to) SchoolYearRange(ComboBox cb)
        {
            if (cb.SelectedIndex <= 0) return (null, null);
            int startYear = int.Parse(cb.SelectedItem.ToString().Split('/')[0]);
            return (new DateTime(startYear, 9, 1),
                    new DateTime(startYear + 1, 8, 31));
        }

        // ═══════════════════════════════════════════════════════
        // ОТЧЁТ 1: Все мероприятия
        // ═══════════════════════════════════════════════════════

        private void Year1_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => LoadReport1Preview();

        private void LoadReport1Preview()
        {
            var (from, to) = SchoolYearRange(cbYear1);
            var pars = new[]
            {
                new SqlParameter("@DateFrom", from != null ? (object)from.Value : DBNull.Value),
                new SqlParameter("@DateTo",   to   != null ? (object)to.Value   : DBNull.Value)
            };
            var dt = DatabaseHelper.ExecProc("sp_Report_AllEvents", pars);
            dgReport1.ItemsSource = ToList(dt);
            int total = 0;
            foreach (DataRow r in dt.Rows) total += Convert.ToInt32(r["KolUchastnikov"]);
            tbSummary1.Text = $"Всего мероприятий: {dt.Rows.Count}   |   Суммарно участников: {total}";
        }

        private void Report1_Click(object sender, RoutedEventArgs e)
        {
            var (from, to) = SchoolYearRange(cbYear1);
            var pars = new[]
            {
                new SqlParameter("@DateFrom", from != null ? (object)from.Value : DBNull.Value),
                new SqlParameter("@DateTo",   to   != null ? (object)to.Value   : DBNull.Value)
            };
            var dt = DatabaseHelper.ExecProc("sp_Report_AllEvents", pars);
            if (dt.Rows.Count == 0) { MessageBox.Show("Нет данных."); return; }

            int total = 0;
            foreach (DataRow r in dt.Rows) total += Convert.ToInt32(r["KolUchastnikov"]);

            string yearLabel = cbYear1.SelectedIndex > 0
                ? $"Учебный год: {cbYear1.SelectedItem}"
                : "Все учебные годы";

            var def = new ReportDefinition
            {
                Title       = "📋 Список всех мероприятий",
                SubTitle    = $"{yearLabel}   |   Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}",
                Data        = dt,
                SummaryLine = $"Итого мероприятий: {dt.Rows.Count}    Суммарно участников: {total}",
                Columns     = new[]
                {
                    new ReportColumn("Nomer",         "№",           8f),
                    new ReportColumn("Nazvanie",       "Название",    38f),
                    new ReportColumn("Data",           "Дата",        20f),
                    new ReportColumn("Vremya",         "Время",       15f),
                    new ReportColumn("Mesto",          "Место",       30f),
                    new ReportColumn("Tip",            "Тип",         25f),
                    new ReportColumn("Organizator",    "Организатор", 42f),
                    new ReportColumn("KolUchastnikov", "Участников",  15f)
                }
            };

            var preview1 = new ReportPreviewWindow { Owner = this };
            preview1.LoadReport(def, "AllEvents");
            preview1.ShowDialog();
        }

        // ═══════════════════════════════════════════════════════
        // ОТЧЁТ 2: Участники с фильтром
        // ═══════════════════════════════════════════════════════

        private void LoadReport2Types()
        {
            var dt    = DatabaseHelper.ExecProc("sp_Dashboard_GetTypes");
            var items = new List<string> { "(все типы)" };
            foreach (DataRow r in dt.Rows) items.Add(r["type_e"].ToString());
            cbType.ItemsSource   = items;
            cbType.SelectedIndex = 0;
        }

        private void Year2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // При выборе учебного года — сбрасываем ручные даты
            dpFrom.SelectedDate = null;
            dpTo.SelectedDate   = null;
            _dateFrom = _dateTo  = null;
            LoadReport2Preview();
        }

        private void LoadReport2Preview()
        {
            var pars = BuildReport2Params();
            var dt   = DatabaseHelper.ExecProc("sp_Report_EventParticipants", pars);
            dgReport2.ItemsSource = ToList(dt);
            tbSummary2.Text = $"Найдено записей: {dt.Rows.Count}";
        }

        private SqlParameter[] BuildReport2Params()
        {
            // Ручные даты перекрывают учебный год
            DateTime? from = _dateFrom, to = _dateTo;
            if (!from.HasValue && !to.HasValue)
            {
                var (y1, y2) = SchoolYearRange(cbYear2);
                from = y1; to = y2;
            }

            string typeVal = cbType.SelectedIndex > 0 ? cbType.SelectedItem?.ToString() : null;

            return new[]
            {
                new SqlParameter("@DateFrom",   from    != null ? (object)from.Value  : DBNull.Value),
                new SqlParameter("@DateTo",     to      != null ? (object)to.Value    : DBNull.Value),
                new SqlParameter("@TypeFilter", typeVal != null ? (object)typeVal     : DBNull.Value)
            };
        }

        private void ApplyFilter2_Click(object sender, RoutedEventArgs e)
        {
            _dateFrom = dpFrom.SelectedDate;
            _dateTo   = dpTo.SelectedDate;
            LoadReport2Preview();
        }

        private void ResetFilter2_Click(object sender, RoutedEventArgs e)
        {
            dpFrom.SelectedDate  = null;
            dpTo.SelectedDate    = null;
            cbType.SelectedIndex = 0;
            _dateFrom = _dateTo  = null;
            cbYear2.SelectedIndex = 1;
            LoadReport2Preview();
        }

        private void Report2_Click(object sender, RoutedEventArgs e)
        {
            var pars = BuildReport2Params();
            var dt   = DatabaseHelper.ExecProc("sp_Report_EventParticipants", pars);
            if (dt.Rows.Count == 0) { MessageBox.Show("Нет данных по выбранному фильтру."); return; }

            var parts = new List<string>();
            string yearLabel = cbYear2.SelectedIndex > 0 ? cbYear2.SelectedItem.ToString() : null;
            if (yearLabel != null && !_dateFrom.HasValue && !_dateTo.HasValue)
                parts.Add($"Учебный год: {yearLabel}");
            if (_dateFrom.HasValue) parts.Add($"с {_dateFrom.Value:dd.MM.yyyy}");
            if (_dateTo.HasValue)   parts.Add($"по {_dateTo.Value:dd.MM.yyyy}");
            string typeVal = cbType.SelectedIndex > 0 ? cbType.SelectedItem?.ToString() : null;
            if (typeVal != null) parts.Add($"тип: {typeVal}");
            string filterStr = parts.Count > 0 ? string.Join(", ", parts) : "Все периоды, все типы";

            var def = new ReportDefinition
            {
                Title       = "🔎 Участники по мероприятиям",
                SubTitle    = filterStr + $"   |   Сформировано: {DateTime.Now:dd.MM.yyyy HH:mm}",
                Data        = dt,
                SummaryLine = $"Итого записей: {dt.Rows.Count}",
                Columns     = new[]
                {
                    new ReportColumn("Meropriyatie", "Мероприятие",  42f),
                    new ReportColumn("Data",         "Дата",         18f),
                    new ReportColumn("Tip",          "Тип",          22f),
                    new ReportColumn("Mesto",        "Место",        25f),
                    new ReportColumn("Organizator",  "Организатор",  38f),
                    new ReportColumn("Uchastnik",    "Участник",     38f),
                    new ReportColumn("Klass",        "Класс",        12f),
                    new ReportColumn("Telefon",      "Телефон",      20f)
                }
            };

            var preview2 = new ReportPreviewWindow { Owner = this };
            preview2.LoadReport(def, "Participants");
            preview2.ShowDialog();
        }

        // ═══════════════════════════════════════════════════════
        // ОТЧЁТ 3: Рейтинг активности
        // ═══════════════════════════════════════════════════════

        private void Year3_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => LoadReport3Preview();

        private void LoadReport3Preview()
        {
            var (from, to) = SchoolYearRange(cbYear3);
            var pars = new[]
            {
                new SqlParameter("@DateFrom", from != null ? (object)from.Value : DBNull.Value),
                new SqlParameter("@DateTo",   to   != null ? (object)to.Value   : DBNull.Value)
            };
            var dt = DatabaseHelper.ExecProc("sp_Report_ParticipantActivity", pars);
            dgReport3.ItemsSource = ToList(dt);
            int active = 0;
            foreach (DataRow r in dt.Rows)
                if (Convert.ToInt32(r["KolMeropriyatiy"]) > 0) active++;
            tbSummary3.Text = $"Всего участников: {dt.Rows.Count}   |   Активных: {active}";
        }

        private void Report3_Click(object sender, RoutedEventArgs e)
        {
            var (from, to) = SchoolYearRange(cbYear3);
            var pars = new[]
            {
                new SqlParameter("@DateFrom", from != null ? (object)from.Value : DBNull.Value),
                new SqlParameter("@DateTo",   to   != null ? (object)to.Value   : DBNull.Value)
            };
            var dt = DatabaseHelper.ExecProc("sp_Report_ParticipantActivity", pars);
            if (dt.Rows.Count == 0) { MessageBox.Show("Нет данных."); return; }

            int active = 0;
            foreach (DataRow r in dt.Rows)
                if (Convert.ToInt32(r["KolMeropriyatiy"]) > 0) active++;

            string yearLabel = cbYear3.SelectedIndex > 0
                ? $"Учебный год: {cbYear3.SelectedItem}"
                : "Все учебные годы";

            var def = new ReportDefinition
            {
                Title       = "📈 Рейтинг активности участников",
                SubTitle    = $"{yearLabel}   |   Сформировано: {DateTime.Now:dd.MM.yyyy HH:mm}",
                Data        = dt,
                SummaryLine = $"Всего: {dt.Rows.Count}    Активных (≥1 мер.): {active}",
                Columns     = new[]
                {
                    new ReportColumn("Mesto",             "Место",          12f),
                    new ReportColumn("Uchastnik",         "Участник",       48f),
                    new ReportColumn("Klass",             "Класс",          12f),
                    new ReportColumn("Email",             "Email",          40f),
                    new ReportColumn("KolMeropriyatiy",   "Мер-тий",        14f),
                    new ReportColumn("PervoeUchastie",    "Первое участие", 25f),
                    new ReportColumn("PosledneeUchastie", "Последнее",      25f)
                }
            };

            var preview3 = new ReportPreviewWindow { Owner = this };
            preview3.LoadReport(def, "Activity");
            preview3.ShowDialog();
        }

        // ── DataTable → List<dynamic> ──────────────────────────────────────────
        private static List<dynamic> ToList(DataTable dt)
        {
            var list = new List<dynamic>();
            foreach (DataRow row in dt.Rows)
            {
                var item = new ExpandoObject() as IDictionary<string, object>;
                foreach (DataColumn col in dt.Columns)
                {
                    var v = row[col];
                    item[col.ColumnName] = (v is DateTime d) ? d.ToString("dd.MM.yyyy")
                                          : (v == DBNull.Value ? "" : v);
                }
                list.Add((ExpandoObject)item);
            }
            return list;
        }
    }
}
