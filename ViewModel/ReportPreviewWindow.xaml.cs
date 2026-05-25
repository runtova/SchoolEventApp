// ReportPreviewWindow.xaml.cs

using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using Microsoft.Web.WebView2.Core;
using SchoolEventApp.Services;

namespace SchoolEventApp
{
    public partial class ReportPreviewWindow : Window
    {
        private string         _pdfPath;
        private string         _baseName  = "Отчёт";
        private ReportColumn[] _columns;
        private DataTable      _rawData;
        private ReportDefinition _pendingDef;
        private string           _pendingBase;

        public ReportPreviewWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
        }

        // ── Инициализация WebView2 ────────────────────────────────────────────
        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                string cacheDir = Path.Combine(Path.GetTempPath(), "SchoolEventWebView2Cache");
                var env = await CoreWebView2Environment.CreateAsync(userDataFolder: cacheDir);
                await webView.EnsureCoreWebView2Async(env);

                // Если LoadReport уже вызвали до инициализации — обрабатываем сейчас
                if (_pendingDef != null)
                {
                    BuildAndShow(_pendingDef, _pendingBase);
                    _pendingDef  = null;
                    _pendingBase = null;
                }
            }
            catch (Exception ex)
            {
                SetStatus("WebView2 недоступен: " + ex.Message);
            }
        }

        // ── Загрузка отчёта ───────────────────────────────────────────────────
        public void LoadReport(ReportDefinition def, string baseName = "report")
        {
            _baseName = baseName;
            _columns  = def.Columns;
            _rawData  = def.Data;

            tbReportTitle.Text = def.Title;
            Title = def.Title;
            SetStatus("Формируется отчёт…");

            // Если WebView2 уже готов — строим сразу, иначе откладываем
            if (webView.CoreWebView2 != null)
                BuildAndShow(def, baseName);
            else
            {
                _pendingDef  = def;
                _pendingBase = baseName;
            }
        }

        private void BuildAndShow(ReportDefinition def, string baseName)
        {
            try
            {
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                var report = ReportService.BuildReport(def);
                _pdfPath   = ReportService.ExportToPdf(report, baseName + "_" + stamp);
                report.Dispose();

                tbRowCount.Text = $"Записей: {_rawData?.Rows.Count ?? 0}";
                SetStatus($"Готово — {Path.GetFileName(_pdfPath)}");

                // Показываем PDF в WebView2
                webView.CoreWebView2.Navigate(new Uri(_pdfPath).AbsoluteUri);
            }
            catch (Exception ex)
            {
                SetStatus("Ошибка: " + ex.Message);
                MessageBox.Show("Ошибка при формировании отчёта:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Печать — открываем PDF в системном просмотрщике (Edge/Adobe)
        //    Пользователь нажимает Ctrl+P или кнопку печати внутри просмотрщика
        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (!HasPdf()) return;
            try
            {
                Process.Start(new ProcessStartInfo(_pdfPath) { UseShellExecute = true });
            }
            catch (Exception ex) { ShowErrorMsg(ex); }
        }

        // ── Сохранить PDF ─────────────────────────────────────────────────────
        private void btnSavePdf_Click(object sender, RoutedEventArgs e)
        {
            if (!HasPdf()) return;
            var dlg = new SaveFileDialog
            {
                Title    = "Сохранить PDF",
                Filter   = "PDF файл (*.pdf)|*.pdf",
                FileName = _baseName + "_" + DateTime.Now.ToString("yyyyMMdd") + ".pdf"
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                File.Copy(_pdfPath, dlg.FileName, overwrite: true);
                SetStatus("PDF сохранён: " + dlg.FileName);
                if (MessageBox.Show("Файл сохранён. Открыть?", "Готово",
                        MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
            }
            catch (Exception ex) { ShowErrorMsg(ex); }
        }

        // ── Экспорт в Word ────────────────────────────────────────────────────
        private void btnExportWord_Click(object sender, RoutedEventArgs e)
        {
            if (!HasData()) return;
            var dlg = new SaveFileDialog
            {
                Title    = "Сохранить как Word",
                Filter   = "Word документ (*.docx)|*.docx",
                FileName = _baseName + "_" + DateTime.Now.ToString("yyyyMMdd") + ".docx"
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                string tmp = ReportService.ExportToWord(_rawData, _baseName, _columns, tbReportTitle.Text);
                File.Copy(tmp, dlg.FileName, overwrite: true);
                SetStatus("Word сохранён: " + dlg.FileName);
                if (MessageBox.Show("Файл сохранён. Открыть?", "Готово",
                        MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
            }
            catch (Exception ex) { ShowErrorMsg(ex); }
        }

        // ── Экспорт в Excel (CSV) ─────────────────────────────────────────────
        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (!HasData()) return;
            var dlg = new SaveFileDialog
            {
                Title    = "Сохранить как Excel",
                Filter   = "CSV файл (*.csv)|*.csv",
                FileName = _baseName + "_" + DateTime.Now.ToString("yyyyMMdd") + ".csv"
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                string tmp = ReportService.ExportToCsv(_rawData, _baseName, _columns, tbReportTitle.Text);
                File.Copy(tmp, dlg.FileName, overwrite: true);
                SetStatus("Excel сохранён: " + dlg.FileName);
                if (MessageBox.Show("Файл сохранён. Открыть в Excel?", "Готово",
                        MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
            }
            catch (Exception ex) { ShowErrorMsg(ex); }
        }

        // ── Закрыть ───────────────────────────────────────────────────────────
        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();

        // ── Вспомогательные ───────────────────────────────────────────────────
        private bool HasPdf()
        {
            if (!string.IsNullOrEmpty(_pdfPath) && File.Exists(_pdfPath)) return true;
            MessageBox.Show("Отчёт ещё не сформирован.", "Внимание",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        private bool HasData()
        {
            if (_rawData != null && _columns != null) return true;
            MessageBox.Show("Данные не загружены.", "Внимание",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        private void SetStatus(string msg) => tbStatus.Text = msg;

        private void ShowErrorMsg(Exception ex) =>
            MessageBox.Show("Ошибка:\n" + ex.Message, "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
