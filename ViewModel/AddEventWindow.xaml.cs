using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace SchoolEventApp
{
    public partial class AddEventWindow : Window
    {
        private bool  _isEdit   = false;
        private short _editCod  = 0;
        private bool  _hideCode = false;

        // Если задан — организатор зафиксирован (режим организатора)
        private short? _fixedOrganizerCod = null;

        // Имя файла, который пользователь выбрал (ещё не скопирован)
        // null  = не менялось (при Edit картинка остаётся прежней)
        private string _pendingSourcePath = null;

        // Флаг: пользователь нажал «Очистить» — передаём '' в процедуру
        private bool _imageCleared = false;

        // ── Конструкторы ──────────────────────────────────────────────────────

        public AddEventWindow()
        { InitializeComponent(); Loaded += OnLoaded; }

        public AddEventWindow(short cod) : this()
        { _isEdit = true; _editCod = cod; }

        public AddEventWindow(bool hideCode, short fixedOrganizerCod) : this()
        {
            _hideCode          = hideCode;
            _fixedOrganizerCod = fixedOrganizerCod;
        }

        public AddEventWindow(short cod, bool hideCode, short fixedOrganizerCod) : this()
        {
            _isEdit            = true;
            _editCod           = cod;
            _hideCode          = hideCode;
            _fixedOrganizerCod = fixedOrganizerCod;
        }

        // ── Загрузка формы ────────────────────────────────────────────────────

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadOrganizers();

            if (_hideCode)
            {
                panelCod.Visibility = System.Windows.Visibility.Collapsed;
                tbCod.Visibility    = System.Windows.Visibility.Collapsed;
            }

            if (_isEdit)
            {
                tbTitle.Text = Title = "Редактирование мероприятия";
                try
                {
                    var dt = DatabaseHelper.ExecProc("sp_Event_GetAll");
                    foreach (DataRow r in dt.Rows)
                    {
                        if (Convert.ToInt16(r["event_cod"]) != _editCod) continue;

                        tbCod.Text          = _editCod.ToString();
                        tbName.Text         = r["event_name"].ToString();
                        dpDate.SelectedDate = Convert.ToDateTime(r["date_e"]);
                        tbTime.Text         = r["time_e"].ToString();
                        tbPlace.Text        = r["place"].ToString();
                        tbType.Text         = r["type_e"].ToString();

                        int maxP = 0;
                        if (dt.Columns.Contains("max_participants") && r["max_participants"] != DBNull.Value)
                            maxP = Convert.ToInt32(r["max_participants"]);
                        tbMaxParticipants.Text = maxP.ToString();

                        // Загружаем превью из папки изображений по имени файла
                        if (dt.Columns.Contains("image_path") && r["image_path"] != DBNull.Value)
                        {
                            string fileName = r["image_path"].ToString();
                            if (!string.IsNullOrWhiteSpace(fileName))
                            {
                                tbImagePath.Text = fileName;
                                LoadImagePreviewFromFile(AppSettings.GetImageFullPath(fileName));
                            }
                        }

                        short orgCod = Convert.ToInt16(r["organizer_cod"]);
                        foreach (var item in cbOrganizer.Items)
                        {
                            if (item is IDictionary<string, object> d &&
                                Convert.ToInt16(d["organizer_cod"]) == orgCod)
                            { cbOrganizer.SelectedItem = item; break; }
                        }
                        break;
                    }
                }
                catch (Exception ex) { ShowError("Ошибка загрузки: " + ex.Message); }
            }
            else
            {
                dpDate.SelectedDate     = DateTime.Today;
                dpDate.DisplayDateStart = DateTime.Today;
                try
                {
                    var dt = DatabaseHelper.ExecProc("sp_Event_GetAll");
                    int maxCod = 0;
                    foreach (DataRow r in dt.Rows)
                    {
                        int c = Convert.ToInt32(r["event_cod"]);
                        if (c > maxCod) maxCod = c;
                    }
                    tbCod.Text = (maxCod + 1).ToString();
                }
                catch { tbCod.Text = "1"; }
            }
        }

        // ── Загрузка списка организаторов ─────────────────────────────────────

        private void LoadOrganizers()
        {
            try
            {
                short? selectedCod = null;
                if (cbOrganizer.SelectedItem is IDictionary<string, object> sel)
                    selectedCod = Convert.ToInt16(sel["organizer_cod"]);

                var dt   = DatabaseHelper.ExecProc("sp_Organizer_GetAll");
                var list = new List<dynamic>();
                foreach (DataRow r in dt.Rows)
                {
                    if (_fixedOrganizerCod.HasValue &&
                        Convert.ToInt16(r["organizer_cod"]) != _fixedOrganizerCod.Value)
                        continue;

                    var item = new ExpandoObject() as IDictionary<string, object>;
                    item["organizer_cod"] = r["organizer_cod"];
                    item["DisplayText"]   = r["organizer_FIO"].ToString();
                    list.Add((ExpandoObject)item);
                }
                cbOrganizer.ItemsSource = list;

                if (_fixedOrganizerCod.HasValue && list.Count == 1)
                {
                    cbOrganizer.SelectedIndex = 0;
                    cbOrganizer.IsEnabled     = false;
                }
                else if (selectedCod.HasValue)
                {
                    foreach (var item in cbOrganizer.Items)
                    {
                        if (item is IDictionary<string, object> d &&
                            Convert.ToInt16(d["organizer_cod"]) == selectedCod.Value)
                        { cbOrganizer.SelectedItem = item; break; }
                    }
                }
            }
            catch (Exception ex) { ShowError("Ошибка загрузки организаторов: " + ex.Message); }
        }

        private void cbOrganizer_DropDownOpened(object sender, EventArgs e)
            => LoadOrganizers();

        // ── Сохранение ────────────────────────────────────────────────────────

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            HideError();

            if (!short.TryParse(tbCod.Text.Trim(), out short cod) || cod <= 0)
            { ShowError("Некорректный код мероприятия."); return; }

            if (string.IsNullOrWhiteSpace(tbName.Text) || tbName.Text.Trim().Length > 50)
            { ShowError("Название обязательно (не более 30 символов)."); return; }

            if (dpDate.SelectedDate == null)
            { ShowError("Выберите дату."); return; }

            if (!TimeSpan.TryParse(tbTime.Text.Trim(), out TimeSpan time))
            { ShowError("Неверный формат времени. Используйте чч:мм."); return; }

            if (string.IsNullOrWhiteSpace(tbPlace.Text) || tbPlace.Text.Trim().Length > 20)
            { ShowError("Место обязательно (не более 20 символов)."); return; }

            if (cbOrganizer.SelectedItem == null)
            { ShowError("Выберите организатора."); return; }

            short orgCod = Convert.ToInt16(
                ((IDictionary<string, object>)cbOrganizer.SelectedItem)["organizer_cod"]);

            if (_fixedOrganizerCod.HasValue && orgCod != _fixedOrganizerCod.Value)
            { ShowError("Нельзя назначить другого организатора."); return; }

            if (string.IsNullOrWhiteSpace(tbType.Text) || tbType.Text.Trim().Length > 20)
            { ShowError("Тип обязателен (не более 20 символов)."); return; }

            if (!int.TryParse(tbMaxParticipants.Text.Trim(), out int maxPart) || maxPart < 0)
            { ShowError("Макс. мест — целое число ≥ 0."); return; }

            // ── Копируем файл в папку изображений и строим значение для БД ────
            //   _pendingSourcePath != null  → пользователь выбрал новый файл
            //   _imageCleared               → пользователь нажал «Очистить»
            //   иначе при Edit              → NULL (процедура оставит старое значение)
            //   иначе при Insert            → NULL (нет картинки)
            object imagePathParam; // передаём в @image_path

            if (_pendingSourcePath != null)
            {
                try
                {
                    string fileName = Path.GetFileName(_pendingSourcePath);
                    string destPath = AppSettings.GetImageFullPath(fileName);

                    // Если файл не уже в папке — копируем
                    if (!string.Equals(_pendingSourcePath, destPath, StringComparison.OrdinalIgnoreCase))
                        File.Copy(_pendingSourcePath, destPath, overwrite: true);

                    imagePathParam = fileName; // в БД только имя файла
                }
                catch (Exception ex)
                {
                    ShowError("Не удалось сохранить изображение: " + ex.Message);
                    return;
                }
            }
            else if (_imageCleared)
            {
                imagePathParam = ""; // процедура интерпретирует '' как «очистить»
            }
            else
            {
                imagePathParam = DBNull.Value; // NULL → процедура не трогает старое значение
            }

            var pars = new[]
            {
                new SqlParameter("@event_name",       tbName.Text.Trim()),
                new SqlParameter("@date_e",           dpDate.SelectedDate.Value),
                new SqlParameter("@time_e",           time),
                new SqlParameter("@place",            tbPlace.Text.Trim()),
                new SqlParameter("@organizer_cod",    orgCod),
                new SqlParameter("@type_e",           tbType.Text.Trim()),
                new SqlParameter("@max_participants", maxPart == 0 ? (object)DBNull.Value : maxPart),
                new SqlParameter("@image_path",       imagePathParam),
            };

            string proc = _isEdit ? "sp_Event_Update" : "sp_Event_Insert";
            if (DatabaseHelper.ExecProcNonQuery(proc, pars))
                DialogResult = true;
        }

        // ── Выбор изображения ─────────────────────────────────────────────────

        private void PickImage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title  = "Выберите картинку для мероприятия",
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Все файлы|*.*"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                _pendingSourcePath = dlg.FileName;
                _imageCleared      = false;
                tbImagePath.Text   = Path.GetFileName(dlg.FileName);
                LoadImagePreviewFromFile(dlg.FileName);
            }
            catch (Exception ex)
            {
                ShowError("Не удалось загрузить изображение: " + ex.Message);
            }
        }

        private void ClearImage_Click(object sender, RoutedEventArgs e)
        {
            _pendingSourcePath = null;
            _imageCleared      = true;
            tbImagePath.Text   = "";
            imgPreview.Source  = null;
        }

        // ── Отображение превью из файла ───────────────────────────────────────

        private void LoadImagePreviewFromFile(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
            {
                imgPreview.Source = null;
                return;
            }
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource        = new Uri(fullPath);
                bmp.CacheOption      = BitmapCacheOption.OnLoad;
                bmp.DecodePixelWidth = 300;
                bmp.EndInit();
                bmp.Freeze();
                imgPreview.Source = bmp;
            }
            catch { imgPreview.Source = null; }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void ShowError(string msg)
        {
            tbError.Text = msg;
            borderError.Visibility = Visibility.Visible;
        }
        private void HideError() => borderError.Visibility = Visibility.Collapsed;
    }
}
