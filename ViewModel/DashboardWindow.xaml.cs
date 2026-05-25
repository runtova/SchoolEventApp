using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;
using System.Windows;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.Defaults;
using SkiaSharp;

namespace SchoolEventApp
{
    public partial class DashboardWindow : Window
    {
        private bool _suppressYearChange = false;

        // Палитра
        private static readonly SKColor[] Palette =
        {
            SKColor.Parse("#1e5943"),
            SKColor.Parse("#3B82F6"),
            SKColor.Parse("#F59E0B"),
            SKColor.Parse("#8B5CF6"),
            SKColor.Parse("#EC4899"),
            SKColor.Parse("#10B981"),
            SKColor.Parse("#F97316"),
        };

        public IEnumerable<ISeries> SeriesByType   { get; private set; }
        public IEnumerable<ISeries> SeriesPie       { get; private set; }
        public IEnumerable<ISeries> SeriesByDate    { get; private set; }
        public IEnumerable<ISeries> SeriesTopOrgs   { get; private set; }

        public IEnumerable<Axis> XAxesByType  { get; private set; }
        public IEnumerable<Axis> YAxesByType  { get; private set; }
        public IEnumerable<Axis> XAxesByDate  { get; private set; }
        public IEnumerable<Axis> YAxesByDate  { get; private set; }
        public IEnumerable<Axis> XAxesTopOrgs { get; private set; }
        public IEnumerable<Axis> YAxesTopOrgs { get; private set; }

        public DashboardWindow()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadSchoolYears();
            LoadTypes();
            ApplyFilter();
        }

        private void LoadSchoolYears()
        {
            _suppressYearChange = true;
            var items = new List<string> { "(все годы)" };
            int cur = DateTime.Today.Month >= 9 ? DateTime.Today.Year : DateTime.Today.Year - 1;
            for (int y = cur; y >= 2020; y--)
                items.Add($"{y}/{y + 1}");
            cbSchoolYear.ItemsSource   = items;
            cbSchoolYear.SelectedIndex = 1;
            _suppressYearChange = false;
        }

        private void SchoolYear_SelectionChanged(object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_suppressYearChange) return;
            if (cbSchoolYear.SelectedIndex > 0)
            {
                dpFrom.SelectedDate = null;
                dpTo.SelectedDate   = null;
            }
            ApplyFilter();
        }

        private (DateTime? from, DateTime? to) GetDateRange()
        {
            if (dpFrom.SelectedDate.HasValue || dpTo.SelectedDate.HasValue)
                return (dpFrom.SelectedDate, dpTo.SelectedDate);
            if (cbSchoolYear.SelectedIndex > 0)
            {
                int y = int.Parse(cbSchoolYear.SelectedItem.ToString().Split('/')[0]);
                return (new DateTime(y, 9, 1), new DateTime(y + 1, 8, 31));
            }
            return (null, null);
        }

        private void LoadTypes()
        {
            try
            {
                var dt    = DatabaseHelper.ExecProc("sp_Dashboard_GetTypes");
                var items = new List<string> { "(все типы)" };
                foreach (DataRow r in dt.Rows)
                    items.Add(r["type_e"].ToString());
                cbType.ItemsSource   = items;
                cbType.SelectedIndex = 0;
            }
            catch { }
        }

        private void ApplyFilter_Click(object sender, RoutedEventArgs e) => ApplyFilter();

        private void ResetFilter_Click(object sender, RoutedEventArgs e)
        {
            _suppressYearChange        = true;
            cbSchoolYear.SelectedIndex = 1;
            _suppressYearChange        = false;
            dpFrom.SelectedDate        = null;
            dpTo.SelectedDate          = null;
            cbType.SelectedIndex       = 0;
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            try
            {
                LoadMetrics(BuildParams());
                BuildByTypeChart(BuildParams());
                BuildPieChart(BuildParams());
                BuildByDateChart(BuildParams());
                BuildTopOrgsChart(BuildParams());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private SqlParameter[] BuildParams()
        {
            var (from, to) = GetDateRange();
            string typeVal = cbType.SelectedIndex > 0 ? cbType.SelectedItem?.ToString() : null;
            return new[]
            {
                new SqlParameter("@DateFrom",   from    != null ? (object)from.Value : DBNull.Value),
                new SqlParameter("@DateTo",     to      != null ? (object)to.Value   : DBNull.Value),
                new SqlParameter("@TypeFilter", typeVal != null ? (object)typeVal    : DBNull.Value),
            };
        }

        private void LoadMetrics(SqlParameter[] pars)
        {
            var dt = DatabaseHelper.ExecProc("sp_Dashboard_GetStats", pars);
            if (dt.Rows.Count == 0) return;
            var r = dt.Rows[0];

            int totalEvents = Convert.ToInt32(r["TotalEvents"]);
            int totalPart   = Convert.ToInt32(r["TotalParticipants"]);

            lblTotalEvents.Text       = totalEvents.ToString();
            lblTotalParticipants.Text = totalPart.ToString();
            lblAvgPerEvent.Text       = totalEvents > 0
                ? $"в среднем {Math.Round((double)totalPart / totalEvents, 1)} чел." : "";
            lblAvgParticipants.Text = "";

            if (r["EarliestDate"] != DBNull.Value)
            {
                lblMinDate.Text = Convert.ToDateTime(r["EarliestDate"]).ToString("dd.MM.yyyy");
                lblMinName.Text = dt.Columns.Contains("EarliestName") && r["EarliestName"] != DBNull.Value
                    ? r["EarliestName"].ToString() : "";
            }
            else { lblMinDate.Text = "—"; lblMinName.Text = ""; }

            if (r["LatestDate"] != DBNull.Value)
            {
                lblMaxDate.Text = Convert.ToDateTime(r["LatestDate"]).ToString("dd.MM.yyyy");
                lblMaxName.Text = dt.Columns.Contains("LatestName") && r["LatestName"] != DBNull.Value
                    ? r["LatestName"].ToString() : "";
            }
            else { lblMaxDate.Text = "—"; lblMaxName.Text = ""; }
        }

        private void BuildByTypeChart(SqlParameter[] pars)
        {
            var dt = DatabaseHelper.ExecProc("sp_Dashboard_ByType", pars);
            var series = new List<ISeries>();
            int ci = 0;

            foreach (DataRow r in dt.Rows)
            {
                series.Add(new RowSeries<int>
                {
                    Values             = new[] { Convert.ToInt32(r["EventCount"]) },
                    Name               = r["EventType"].ToString(),
                    Fill               = new SolidColorPaint(Palette[ci % Palette.Length]),
                    Stroke             = null,
                    MaxBarWidth        = 30,
                    DataLabelsPaint    = new SolidColorPaint(SKColors.White),
                    DataLabelsSize     = 12,
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Middle,
                });
                ci++;
            }

            chartByType.Series = series;
            chartByType.XAxes  = new[] { new Axis
            {
                MinLimit        = 0,
                TextSize        = 11,
                LabelsPaint     = new SolidColorPaint(SKColor.Parse("#9CA3AF")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#F3F4F6")),
            }};
            chartByType.YAxes = new[] { new Axis
            {
                Labels          = new[] { "" },
                TextSize        = 11,
                LabelsPaint     = new SolidColorPaint(SKColor.Parse("#4B5563")),
                SeparatorsPaint = null,
            }};
        }

        private void BuildPieChart(SqlParameter[] pars)
        {
            var dt     = DatabaseHelper.ExecProc("sp_Dashboard_ByType", pars);
            var series = new List<ISeries>();
            int ci = 0, total = 0;
            foreach (DataRow r in dt.Rows) total += Convert.ToInt32(r["EventCount"]);

            foreach (DataRow r in dt.Rows)
            {
                int count = Convert.ToInt32(r["EventCount"]);
                int pct   = total > 0 ? (int)Math.Round(100.0 * count / total) : 0;
                series.Add(new PieSeries<int>
                {
                    Values          = new[] { count },
                    Name            = $"{r["EventType"]} ({pct}%)",
                    Fill            = new SolidColorPaint(Palette[ci % Palette.Length]),
                    Stroke          = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 },
                    InnerRadius     = 50,
                    DataLabelsPaint = null,
                });
                ci++;
            }
            chartPie.Series = series;
        }

        private void BuildByDateChart(SqlParameter[] pars)
        {
            var dt     = DatabaseHelper.ExecProc("sp_Dashboard_ByDate", pars);
            var values = new List<ObservableValue>();
            var labels = new List<string>();

            foreach (DataRow r in dt.Rows)
            {
                labels.Add(Convert.ToDateTime(r["EventDate"]).ToString("dd.MM"));
                values.Add(new ObservableValue(Convert.ToDouble(r["EventCount"])));
            }

            double maxVal = values.Count > 0 ? values.Max(v => v.Value ?? 0) : 1;

            chartByDate.Series = new ISeries[]
            {
                new LineSeries<ObservableValue>
                {
                    Values             = values,
                    Name               = "Мероприятий",
                    Stroke             = new SolidColorPaint(SKColor.Parse("#1e5943")) { StrokeThickness = 2.5f },
                    Fill               = null,
                    GeometryFill       = new SolidColorPaint(SKColor.Parse("#1e5943")),
                    GeometryStroke     = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 },
                    GeometrySize       = 8,
                    LineSmoothness     = 0.4,
                    DataLabelsPaint    = new SolidColorPaint(SKColor.Parse("#374151")),
                    DataLabelsSize     = 10,
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top,
                }
            };
            chartByDate.XAxes = new[] { new Axis
            {
                Labels          = labels,
                TextSize        = 10,
                LabelsPaint     = new SolidColorPaint(SKColor.Parse("#9CA3AF")),
                SeparatorsPaint = null,
                LabelsRotation  = -30,
            }};
            chartByDate.YAxes = new[] { new Axis
            {
                MinLimit        = 0,
                MaxLimit        = maxVal < 2 ? maxVal + 2 : maxVal * 1.4,
                TextSize        = 11,
                LabelsPaint     = new SolidColorPaint(SKColor.Parse("#9CA3AF")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#F3F4F6")),
            }};
        }

        private void BuildTopOrgsChart(SqlParameter[] pars)
        {
            DataTable dt;
            try { dt = DatabaseHelper.ExecProc("sp_Dashboard_TopOrganizers", pars); }
            catch { return; }
            if (dt.Rows.Count == 0) { chartTopOrgs.Series = null; return; }

            // Каждый организатор — отдельный RowSeries с одним значением.
            // Имя отображается через встроенную легенду LiveCharts (LegendPosition="Right" в XAML).
            // Ось Y скрывается — при одиночных сериях позиции Y всегда = 0 и подписи бессмысленны.
            var seriesList = new List<ISeries>();
            int ci = 0;

            foreach (DataRow r in dt.Rows)
            {
                string full    = r["OrganizerName"].ToString();
                string[] parts = full.Trim().Split(' ');
                string label   = parts.Length >= 2
                    ? $"{parts[0]} {string.Join("", parts.Skip(1).Select(p => p.Length > 0 ? p[0] + "." : ""))}"
                    : full;

                seriesList.Add(new RowSeries<int>
                {
                    Values             = new[] { Convert.ToInt32(r["ParticipantCount"]) },
                    Name               = label,
                    Fill               = new SolidColorPaint(Palette[ci % Palette.Length]),
                    Stroke             = null,
                    MaxBarWidth        = 32,
                    DataLabelsPaint    = new SolidColorPaint(SKColors.White),
                    DataLabelsSize     = 12,
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Middle,
                });
                ci++;
            }

            int n = seriesList.Count;
            chartTopOrgs.Height = Math.Max(200, n * 52 + 40);
            chartTopOrgs.Series = seriesList;

            chartTopOrgs.XAxes = new[] { new Axis
            {
                MinLimit        = 0,
                TextSize        = 11,
                LabelsPaint     = new SolidColorPaint(SKColor.Parse("#9CA3AF")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#F3F4F6")),
            }};

            chartTopOrgs.YAxes = new[] { new Axis
            {
                IsVisible       = false,
                SeparatorsPaint = null,
            }};
        }
    }
}