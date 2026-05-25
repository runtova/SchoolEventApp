using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Windows;
using ClosedXML.Excel;
using Microsoft.Win32;
using SchoolEventApp.Services;

namespace SchoolEventApp
{
    public partial class ParticipantsWindow : Window
    {
        private readonly short  _eventCod;
        private readonly string _eventName;
        private List<dynamic>   _list = new List<dynamic>();

        public ParticipantsWindow(short eventCod, string eventName)
        {
            InitializeComponent();
            _eventCod  = eventCod;
            _eventName = eventName;
            Loaded    += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Title        = "Участники: " + _eventName;
            tbTitle.Text = _eventName;

            var dt = DatabaseHelper.ExecProc("sp_Participation_GetAll");
            int i  = 1;

            foreach (DataRow r in dt.Rows)
            {
                if (Convert.ToInt16(r["event_cod"]) != _eventCod) continue;
                var item = new ExpandoObject() as IDictionary<string, object>;
                item["RowNumber"] = i++;
                foreach (DataColumn col in dt.Columns)
                    item[col.ColumnName] = r[col];
                _list.Add((ExpandoObject)item);
            }

            dgParticipants.ItemsSource = _list;
            tbCount.Text = "(" + _list.Count + " чел.)";
        }

        // ── Экспорт в Excel (ClosedXML) ───────────────────────────────────────

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_list.Count == 0)
            {
                MessageBox.Show("Нет участников для экспорта.", "Экспорт",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Title    = "Сохранить список участников",
                Filter   = "Excel файл (*.xlsx)|*.xlsx",
                FileName = "Участники_" + SanitizeFileName(_eventName) + "_" + DateTime.Today.ToString("dd.MM.yyyy") + ".xlsx"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("Участники");

                    ws.Cell(1, 1).Value = "Участники мероприятия: " + _eventName;
                    ws.Cell(1, 1).Style.Font.Bold     = true;
                    ws.Cell(1, 1).Style.Font.FontSize = 14;
                    ws.Range(1, 1, 1, 5).Merge();

                    ws.Cell(2, 1).Value = "Дата формирования: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                    ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;
                    ws.Range(2, 1, 2, 5).Merge();

                    string[] headers = { "№", "ФИО", "Класс", "Телефон", "Email" };
                    for (int c = 0; c < headers.Length; c++)
                    {
                        var cell = ws.Cell(4, c + 1);
                        cell.Value = headers[c];
                        cell.Style.Font.Bold            = true;
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
                        cell.Style.Font.FontColor       = XLColor.White;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }

                    int row = 5;
                    foreach (IDictionary<string, object> p in _list)
                    {
                        ws.Cell(row, 1).Value = p["RowNumber"].ToString();
                        ws.Cell(row, 2).Value = p.ContainsKey("student_FIO")    ? p["student_FIO"].ToString()    : "";
                        ws.Cell(row, 3).Value = p.ContainsKey("class")          ? p["class"].ToString()          : "";
                        ws.Cell(row, 4).Value = p.ContainsKey("phone_number_s") ? p["phone_number_s"].ToString() : "";
                        ws.Cell(row, 5).Value = p.ContainsKey("email_s")        ? p["email_s"].ToString()        : "";

                        if (row % 2 == 0)
                            ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#F9FAFB");

                        row++;
                    }

                    ws.Columns().AdjustToContents();
                    ws.Column(1).Width = 6;
                    wb.SaveAs(dlg.FileName);
                }

                MessageBox.Show("Файл сохранён:\n" + dlg.FileName, "Экспорт выполнен",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при экспорте:\n" + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Экспорт в PDF (через ReportService) ──────────────────────────────

        private void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            if (_list.Count == 0)
            {
                MessageBox.Show("Нет участников для экспорта.", "Экспорт",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Title    = "Сохранить список участников",
                Filter   = "PDF файл (*.pdf)|*.pdf",
                FileName = "Участники_" + SanitizeFileName(_eventName) + "_" + DateTime.Today.ToString("dd.MM.yyyy") + ".pdf"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                // Строим DataTable из нашего списка
                var dt = new DataTable();
                dt.Columns.Add("Nomer",  typeof(string));
                dt.Columns.Add("FIO",    typeof(string));
                dt.Columns.Add("Klass",  typeof(string));
                dt.Columns.Add("Phone",  typeof(string));
                dt.Columns.Add("Email",  typeof(string));

                foreach (IDictionary<string, object> p in _list)
                {
                    dt.Rows.Add(
                        p["RowNumber"].ToString(),
                        p.ContainsKey("student_FIO")    ? p["student_FIO"].ToString()    : "",
                        p.ContainsKey("class")          ? p["class"].ToString()          : "",
                        p.ContainsKey("phone_number_s") ? p["phone_number_s"].ToString() : "",
                        p.ContainsKey("email_s")        ? p["email_s"].ToString()        : ""
                    );
                }

                var def = new ReportDefinition
                {
                    Title       = "Участники мероприятия: " + _eventName,
                    SubTitle    = "Сформировано: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm") + "   Всего: " + _list.Count + " чел.",
                    Data        = dt,
                    SummaryLine = "Итого участников: " + _list.Count,
                    Columns     = new[]
                    {
                        new ReportColumn("Nomer", "№",        8f),
                        new ReportColumn("FIO",   "ФИО",     55f),
                        new ReportColumn("Klass", "Класс",   18f),
                        new ReportColumn("Phone", "Телефон", 35f),
                        new ReportColumn("Email", "Email",   45f)
                    }
                };

                var report = ReportService.BuildReport(def);
                report.Prepare();
                string tempPath = ReportService.ExportToPdf(report, "participants_temp");
                System.IO.File.Copy(tempPath, dlg.FileName, overwrite: true);

                MessageBox.Show("PDF сохранён:\n" + dlg.FileName, "Экспорт выполнен",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при экспорте:\n" + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Length > 40 ? name.Substring(0, 40) : name;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
