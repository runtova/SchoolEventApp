// ============================================================
//  ReportService.cs — FastReport.OpenSource 2026 compatible
//  Строит отчёт кодом, без .frx дизайнера
//  Экспорт: PDF · CSV (Excel) · HTML · Печать через PDF
// ============================================================
using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using FastReport;
using FastReport.Export.Html;
using FastReport.Export.PdfSimple;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using OxmlWord = DocumentFormat.OpenXml.Wordprocessing;

namespace SchoolEventApp.Services
{
    public record ReportColumn(string DataField, string Header, float Width);

    public class ReportDefinition
    {
        public string         Title       { get; set; } = "Отчёт";
        public string         SubTitle    { get; set; } = "";
        public DataTable      Data        { get; set; }
        public ReportColumn[] Columns     { get; set; }
        public string         SummaryLine { get; set; } = "";
    }

    public static class ReportService
    {
        private static readonly string TempDir =
            Path.Combine(Path.GetTempPath(), "SchoolEventReports");

        static ReportService()
        {
            Directory.CreateDirectory(TempDir);
            FastReport.Utils.Config.WebMode = false;
        }

        // ── Построить отчёт ───────────────────────────────────────────────────
        public static Report BuildReport(ReportDefinition def)
        {
            var report = new Report();
            report.ReportInfo.Name = def.Title;

            // Источник данных
            var ds = new DataSet();
            var copy = def.Data.Copy();
            copy.TableName = "ReportData";
            ds.Tables.Add(copy);
            report.RegisterData(ds);
            report.GetDataSource("ReportData").Enabled = true;

            // Страница A4 альбомная (единицы — мм * 3.78 = пиксели в FR)
            var page = new ReportPage { Name = "Page1" };
            report.Pages.Add(page);
            page.PaperWidth  = 297f;
            page.PaperHeight = 210f;
            page.LeftMargin  = 10f;
            page.RightMargin = 10f;
            page.TopMargin   = 10f;
            page.BottomMargin = 10f;

            // 1 мм = 3.7795275591 единиц FastReport
            float mm = 3.7795275591f;
            float pageW = (page.PaperWidth - page.LeftMargin - page.RightMargin) * mm;

            // ── Заголовок (ReportTitleBand) ───────────────────────────────────
            var titleBand = new ReportTitleBand { Name = "ReportTitle", Height = 26 * mm };
            page.ReportTitle = titleBand;

            titleBand.Objects.Add(new TextObject
            {
                Name = "TitleText", Text = def.Title,
                Left = 0, Top = 0, Width = pageW, Height = 14 * mm,
                HorzAlign = HorzAlign.Center, VertAlign = VertAlign.Center,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                TextColor = Color.FromArgb(44, 62, 80)
            });

            if (!string.IsNullOrEmpty(def.SubTitle))
                titleBand.Objects.Add(new TextObject
                {
                    Name = "SubText", Text = def.SubTitle,
                    Left = 0, Top = 15 * mm, Width = pageW, Height = 9 * mm,
                    HorzAlign = HorzAlign.Center, VertAlign = VertAlign.Center,
                    Font = new Font("Segoe UI", 9, FontStyle.Italic),
                    TextColor = Color.Gray
                });

            // ── Масштаб колонок ───────────────────────────────────────────────
            float totalGiven = 0;
            foreach (var col in def.Columns) totalGiven += col.Width;
            float scale = (totalGiven > 0 && totalGiven < (pageW / mm))
                ? (pageW / mm) / totalGiven : 1f;

            // ── DataBand (строки) + шапка через первую строку ─────────────────
            var dataBand = new DataBand
            {
                Name       = "DataBand1",
                Height     = 8 * mm,
                DataSource = report.GetDataSource("ReportData")
            };
            page.Bands.Add(dataBand);

            // ── Шапка колонок через GroupHeaderBand ───────────────────────────
            var groupHeader = new GroupHeaderBand
            {
                Name      = "ColHeader",
                Height    = 9 * mm,
                Condition = "1" // всегда одна группа = шапка один раз
            };
            // Вставляем groupHeader ДО dataBand
            page.Bands.Insert(page.Bands.IndexOf(dataBand), groupHeader);

            float x = 0;
            foreach (var col in def.Columns)
            {
                float w = col.Width * scale * mm;

                // Заголовок колонки
                var hdr = new TextObject
                {
                    Name = "Hdr_" + col.DataField, Text = col.Header,
                    Left = x, Top = 0, Width = w, Height = 9 * mm,
                    HorzAlign = HorzAlign.Center, VertAlign = VertAlign.Center,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    TextColor = Color.White,
                    FillColor = Color.FromArgb(44, 62, 80)
                };
                hdr.Border.Lines = BorderLines.All;
                hdr.Border.Color = Color.FromArgb(30, 50, 65);
                groupHeader.Objects.Add(hdr);

                // Ячейка данных
                var cell = new TextObject
                {
                    Name = "Cell_" + col.DataField,
                    Text = $"[ReportData.{col.DataField}]",
                    Left = x, Top = 0, Width = w, Height = 8 * mm,
                    HorzAlign = HorzAlign.Left, VertAlign = VertAlign.Center,
                    Font = new Font("Segoe UI", 9),
                    TextColor = Color.Black
                };
                cell.Border.Lines = BorderLines.All;
                cell.Border.Color = Color.FromArgb(200, 205, 210);
                dataBand.Objects.Add(cell);

                x += w;
            }

            // ── Итоговая строка ───────────────────────────────────────────────
            if (!string.IsNullOrEmpty(def.SummaryLine))
            {
                var summaryBand = new ReportSummaryBand { Name = "Summary", Height = 10 * mm };
                page.ReportSummary = summaryBand;
                summaryBand.Objects.Add(new TextObject
                {
                    Name = "SumText", Text = def.SummaryLine,
                    Left = 0, Top = 0, Width = pageW, Height = 10 * mm,
                    HorzAlign = HorzAlign.Right, VertAlign = VertAlign.Center,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    TextColor = Color.FromArgb(44, 62, 80)
                });
            }

            // ── Нижний колонтитул ─────────────────────────────────────────────
            var footer = new PageFooterBand { Name = "PageFooter", Height = 6 * mm };
            page.PageFooter = footer;
            footer.Objects.Add(new TextObject
            {
                Name = "PageNum",
                Text = "Стр. [Page] из [TotalPages]   |   " + DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                Left = 0, Top = 0, Width = pageW, Height = 6 * mm,
                HorzAlign = HorzAlign.Right, VertAlign = VertAlign.Center,
                Font = new Font("Segoe UI", 8), TextColor = Color.Gray
            });

            report.Prepare();
            return report;
        }

        // ── Экспорт в PDF ─────────────────────────────────────────────────────
        public static string ExportToPdf(Report report, string fileNameWithoutExt)
        {
            string path = Path.Combine(TempDir, fileNameWithoutExt + ".pdf");
            var export = new PDFSimpleExport { ShowProgress = false };
            report.Export(export, path);
            return path;
        }

        // ── Экспорт в HTML ────────────────────────────────────────────────────
        public static string ExportToHtml(Report report, string fileNameWithoutExt)
        {
            string path = Path.Combine(TempDir, fileNameWithoutExt + ".html");
            var export = new HTMLExport
            {
                ShowProgress  = false,
                SinglePage    = true,
                Navigator     = false,
                EmbedPictures = true
            };
            report.Export(export, path);
            return path;
        }

        // ── Экспорт в CSV ─────────────────────────────────────────────────────
        public static string ExportToCsv(DataTable dt, string fileNameWithoutExt,
                                         ReportColumn[] columns, string title)
        {
            string path = Path.Combine(TempDir, fileNameWithoutExt + ".csv");
            using var sw = new StreamWriter(path, false, System.Text.Encoding.UTF8);
            sw.Write('\uFEFF'); // BOM для Excel
            sw.WriteLine("# " + title);
            sw.WriteLine("# Сформировано: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
            sw.WriteLine();

            var hdrs = new System.Text.StringBuilder();
            foreach (var col in columns) hdrs.Append(Esc(col.Header) + ";");
            sw.WriteLine(hdrs.ToString().TrimEnd(';'));

            foreach (DataRow row in dt.Rows)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var col in columns)
                {
                    object val = row.Table.Columns.Contains(col.DataField) ? row[col.DataField] : "";
                    sb.Append(Esc(val?.ToString() ?? "") + ";");
                }
                sw.WriteLine(sb.ToString().TrimEnd(';'));
            }
            return path;
        }

        private static string Esc(string s) =>
            (s.Contains(';') || s.Contains('"') || s.Contains('\n'))
                ? "\"" + s.Replace("\"", "\"\"") + "\""
                : s;

        // ── Печать: открываем PDF в стандартном просмотрщике (диалог печати) ──
        public static void Print(string pdfPath)
        {
            if (string.IsNullOrEmpty(pdfPath) || !File.Exists(pdfPath)) return;
            // Открываем PDF через shell — Windows покажет диалог печати
            Process.Start(new ProcessStartInfo(pdfPath)
            {
                UseShellExecute = true,
                Verb = "print"
            });
        }

        // ── Экспорт в Word (.docx) ────────────────────────────────────────────
        public static string ExportToWord(DataTable dt, string fileNameWithoutExt,
                                           ReportColumn[] columns, string title)
        {
            string path = Path.Combine(TempDir, fileNameWithoutExt + "_" +
                          DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".docx");

            using var wordDoc = WordprocessingDocument.Create(
                path, WordprocessingDocumentType.Document);

            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new OxmlWord.Document();
            var body = mainPart.Document.AppendChild(new OxmlWord.Body());

            // Заголовок
            var titlePara = body.AppendChild(new OxmlWord.Paragraph());
            var titlePPr  = titlePara.AppendChild(new OxmlWord.ParagraphProperties());
            titlePPr.AppendChild(new OxmlWord.Justification { Val = OxmlWord.JustificationValues.Center });
            var titleRun  = titlePara.AppendChild(new OxmlWord.Run());
            var titleRpr  = titleRun.AppendChild(new OxmlWord.RunProperties());
            titleRpr.AppendChild(new OxmlWord.Bold());
            titleRpr.AppendChild(new OxmlWord.FontSize { Val = "28" });
            titleRun.AppendChild(new OxmlWord.Text(title));

            // Дата формирования
            var datePara = body.AppendChild(new OxmlWord.Paragraph());
            var datePPr  = datePara.AppendChild(new OxmlWord.ParagraphProperties());
            datePPr.AppendChild(new OxmlWord.Justification { Val = OxmlWord.JustificationValues.Center });
            var dateRun  = datePara.AppendChild(new OxmlWord.Run());
            var dateRpr  = dateRun.AppendChild(new OxmlWord.RunProperties());
            dateRpr.AppendChild(new OxmlWord.Color { Val = "888888" });
            dateRun.AppendChild(new OxmlWord.Text(
                "Сформировано: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm")));

            // Пустая строка
            body.AppendChild(new OxmlWord.Paragraph());

            // Таблица
            var table = body.AppendChild(new OxmlWord.Table());

            // Ширина таблицы на всю страницу
            var tblPr = table.AppendChild(new OxmlWord.TableProperties());
            tblPr.AppendChild(new OxmlWord.TableWidth
            {
                Type  = OxmlWord.TableWidthUnitValues.Pct,
                Width = "5000"
            });

            // Границы таблицы
            var tblBorders = tblPr.AppendChild(new OxmlWord.TableBorders());
            void SetBorder(OxmlWord.BorderType b)
            {
                b.Val   = OxmlWord.BorderValues.Single;
                b.Size  = 4;
                b.Color = "AAAAAA";
                tblBorders.AppendChild(b);
            }
            SetBorder(new OxmlWord.TopBorder());
            SetBorder(new OxmlWord.BottomBorder());
            SetBorder(new OxmlWord.LeftBorder());
            SetBorder(new OxmlWord.RightBorder());
            SetBorder(new OxmlWord.InsideHorizontalBorder());
            SetBorder(new OxmlWord.InsideVerticalBorder());

            // Шапка таблицы
            var headerRow = table.AppendChild(new OxmlWord.TableRow());
            foreach (var col in columns)
            {
                var cell    = headerRow.AppendChild(new OxmlWord.TableCell());
                var cellPPr = new OxmlWord.TableCellProperties();
                cellPPr.AppendChild(new OxmlWord.Shading
                {
                    Val   = OxmlWord.ShadingPatternValues.Clear,
                    Fill  = "2C3E50",
                    Color = "auto"
                });
                cell.AppendChild(cellPPr);

                var para = cell.AppendChild(new OxmlWord.Paragraph());
                para.AppendChild(new OxmlWord.ParagraphProperties())
                    .AppendChild(new OxmlWord.Justification
                    {
                        Val = OxmlWord.JustificationValues.Center
                    });
                var run = para.AppendChild(new OxmlWord.Run());
                var rpr = run.AppendChild(new OxmlWord.RunProperties());
                rpr.AppendChild(new OxmlWord.Bold());
                rpr.AppendChild(new OxmlWord.Color { Val = "FFFFFF" });
                run.AppendChild(new OxmlWord.Text(col.Header));
            }

            // Строки данных
            bool shade = false;
            foreach (DataRow row in dt.Rows)
            {
                var dataRow = table.AppendChild(new OxmlWord.TableRow());
                foreach (var col in columns)
                {
                    string val = dt.Columns.Contains(col.DataField)
                        ? row[col.DataField]?.ToString() ?? ""
                        : "";

                    var cell = dataRow.AppendChild(new OxmlWord.TableCell());
                    if (shade)
                    {
                        var tcp = new OxmlWord.TableCellProperties();
                        tcp.AppendChild(new OxmlWord.Shading
                        {
                            Val   = OxmlWord.ShadingPatternValues.Clear,
                            Fill  = "F4F6F8",
                            Color = "auto"
                        });
                        cell.AppendChild(tcp);
                    }
                    cell.AppendChild(new OxmlWord.Paragraph())
                        .AppendChild(new OxmlWord.Run())
                        .AppendChild(new OxmlWord.Text(val));
                }
                shade = !shade;
            }

            mainPart.Document.Save();
            return path;
        }

        public static void CleanTemp()
        {
            try { foreach (var f in Directory.GetFiles(TempDir)) File.Delete(f); } catch { }
        }
    }
}
