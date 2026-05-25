using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SchoolEventApp
{
    public partial class UserWindow : Window
    {
        private short _studentCod = -1;

        private Border _selectedCard  = null;
        private IDictionary<string, object> _selectedEvent = null;

        private readonly SearchDebounce _debounce;

        // Текущий фильтр по типу: "" = все, иначе подстрока типа
        private string _typeFilter = "";

        public UserWindow()
        {
            InitializeComponent();
            _debounce = new SearchDebounce(350, async () => await LoadEventsAsync(tbSearchEvent.Text.Trim()));
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            tbCurrentUser.Text = $"👤 {LoginWindow.CurrentFullName}";
            await Task.Run(() => FindStudentCod());
            await LoadEventsAsync();
            await LoadMyEventsAsync();
        }

        // ── Участник ─────────────────────────────────────────────────────────

        private void FindStudentCod()
        {
            // ✅ Берём student_cod напрямую из Users (через LoginWindow)
            // Больше не ищем по имени — имя может быть на другом языке
            _studentCod = LoginWindow.CurrentStudentCod;

            if (_studentCod == -1)
                Dispatcher.Invoke(() =>
                    MessageBox.Show(
                        "Ваш профиль участника не найден в базе данных.\n" +
                        "Обратитесь к администратору чтобы вас добавили как участника.",
                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning));
        }

        // ── Загрузка карточек — только предстоящие мероприятия ───────────────

        private async Task LoadEventsAsync(string search = "")
        {
            var today = DateTime.Today;

            var (rows, _) = await Task.Run(() =>
            {
                DataTable dt = string.IsNullOrEmpty(search)
                    ? DatabaseHelper.ExecProc("sp_Event_GetAll")
                    : DatabaseHelper.ExecProc("sp_Event_Search",
                        new[] { new SqlParameter("@SearchText", search) });

                var map = new Dictionary<short, string>();
                foreach (DataRow r in DatabaseHelper.ExecProc("sp_Organizer_GetAll").Rows)
                    map[Convert.ToInt16(r["organizer_cod"])] = r["organizer_FIO"].ToString();

                var list = new List<IDictionary<string, object>>();
                int i = 1;
                string tf = _typeFilter; // capture for thread
                foreach (DataRow row in dt.Rows)
                {
                    if (row["date_e"] is DateTime d && d.Date < today)
                        continue;

                    // Фильтр по типу
                    if (!string.IsNullOrEmpty(tf))
                    {
                        string rowType = row["type_e"]?.ToString()?.ToLower() ?? "";
                        if (!rowType.Contains(tf)) continue;
                    }

                    var item = new ExpandoObject() as IDictionary<string, object>;
                    item["RowNumber"] = i++;
                    foreach (DataColumn col in dt.Columns)
                        item[col.ColumnName] = row[col];
                    short orgCod = Convert.ToInt16(row["organizer_cod"]);
                    item["organizer_FIO"] = map.ContainsKey(orgCod) ? map[orgCod] : "";
                    list.Add(item);
                }
                return (list, map);
            });

            wpEvents.Children.Clear();
            _selectedCard  = null;
            _selectedEvent = null;

            var cards = new List<(Border card, Image img, string path)>();

            foreach (var item in rows)
            {
                string imgRel = item.ContainsKey("image_path") && item["image_path"] != DBNull.Value
                                ? item["image_path"]?.ToString() ?? "" : "";

                var (card, imgControl) = BuildCardShell(item);
                wpEvents.Children.Add(card);
                cards.Add((card, imgControl, imgRel));
            }

            tbEventCount.Text = $"Предстоящих мероприятий: {rows.Count}";

            foreach (var (card, imgControl, imgRel) in cards)
            {
                if (string.IsNullOrEmpty(imgRel)) continue;
                BitmapImage bmp = await Task.Run(() => LoadBitmapBackground(imgRel));
                if (bmp != null) imgControl.Source = bmp;
            }
        }

        // ── Построение карточки ───────────────────────────────────────────────

        private (Border card, Image imgControl) BuildCardShell(IDictionary<string, object> item)
        {
            string name  = item["event_name"]?.ToString()     ?? "";
            string place = item["place"]?.ToString()          ?? "";
            string type  = item["type_e"]?.ToString()         ?? "";
            string org   = item["organizer_FIO"]?.ToString()  ?? "";
            string date  = item.ContainsKey("date_e") && item["date_e"] is DateTime dt
                           ? dt.ToString("dd.MM.yyyy") : "";
            string time  = item["time_e"]?.ToString()         ?? "";

            int maxP   = item.ContainsKey("max_participants") && item["max_participants"] is not DBNull
                         && int.TryParse(item["max_participants"]?.ToString(), out int mp) ? mp : 0;
            int taken  = item.ContainsKey("participants_count") && item["participants_count"] is not DBNull
                         && int.TryParse(item["participants_count"]?.ToString(), out int tc) ? tc : 0;
            int free   = maxP > 0 ? Math.Max(0, maxP - taken) : -1;

            bool isPast = item.ContainsKey("date_e") && item["date_e"] is DateTime evtDate
                          && evtDate.Date < DateTime.Today;

            var placeholder = new TextBlock
            {
                Text                = "🖼",
                FontSize            = 42,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
                Foreground          = new SolidColorBrush(Color.FromRgb(160, 180, 200))
            };

            var imgControl = new Image { Stretch = Stretch.Uniform, Height = 140 };
            RenderOptions.SetBitmapScalingMode(imgControl, BitmapScalingMode.HighQuality);

            var imgGrid = new Grid { Height = 140 };
            imgGrid.Children.Add(placeholder);
            imgGrid.Children.Add(imgControl);

            var imgBorder = new Border
            {
                Height       = 140,
                ClipToBounds = true,
                CornerRadius = new CornerRadius(8, 8, 0, 0),
                Background   = new SolidColorBrush(Color.FromRgb(220, 230, 240)),
                Child        = imgGrid
            };

            var imgStack = new Grid();
            imgStack.Children.Add(imgBorder);
            if (isPast)
            {
                imgStack.Children.Add(new Border
                {
                    Background          = new SolidColorBrush(Color.FromArgb(200, 100, 100, 110)),
                    CornerRadius        = new CornerRadius(4),
                    Padding             = new Thickness(7, 3, 7, 3),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment   = VerticalAlignment.Top,
                    Margin              = new Thickness(8, 8, 0, 0),
                    Child               = new TextBlock
                    {
                        Text       = "✓ Прошло",
                        FontSize   = 10,
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.SemiBold
                    }
                });
            }

            var typeBadge = new Border
            {
                Background          = TypeColor(type),
                CornerRadius        = new CornerRadius(4),
                Padding             = new Thickness(6, 2, 6, 2),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin              = new Thickness(0, 0, 0, 6),
                Child               = new TextBlock
                {
                    Text       = type,
                    FontSize   = 10,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold
                }
            };

            var info = new StackPanel { Margin = new Thickness(10, 8, 10, 10) };
            info.Children.Add(typeBadge);
            info.Children.Add(new TextBlock
            {
                Text         = name,
                FontSize     = 13,
                FontWeight   = FontWeights.Bold,
                Foreground   = new SolidColorBrush(Color.FromRgb(30, 40, 60)),
                TextWrapping = TextWrapping.Wrap,
                MaxHeight    = 40,
                Margin       = new Thickness(0, 0, 0, 4)
            });
            info.Children.Add(new TextBlock
            {
                Text       = $"📅 {date}  🕐 {time}",
                FontSize   = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 110, 130)),
                Margin     = new Thickness(0, 0, 0, 2)
            });
            info.Children.Add(new TextBlock
            {
                Text         = $"📍 {place}",
                FontSize     = 11,
                Foreground   = new SolidColorBrush(Color.FromRgb(100, 110, 130)),
                Margin       = new Thickness(0, 0, 0, 2),
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            info.Children.Add(new TextBlock
            {
                Text         = $"👤 {org}",
                FontSize     = 10,
                Foreground   = new SolidColorBrush(Color.FromRgb(130, 140, 160)),
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin       = new Thickness(0, 4, 0, 0)
            });

            if (!isPast)
            {
                string seatsText;
                Color  seatsColor;

                if (free == -1)
                {
                    seatsText  = "Мест: без ограничений";
                    seatsColor = Color.FromRgb(100, 160, 100);
                }
                else if (free == 0)
                {
                    seatsText  = "Мест нет";
                    seatsColor = Color.FromRgb(180, 60, 60);
                }
                else if (free <= 5)
                {
                    seatsText  = $"Осталось мест: {free}";
                    seatsColor = Color.FromRgb(200, 120, 30);
                }
                else
                {
                    seatsText  = $"Свободно мест: {free}";
                    seatsColor = Color.FromRgb(39, 140, 90);
                }

                info.Children.Add(new Border
                {
                    Background          = new SolidColorBrush(Color.FromArgb(20,
                                              seatsColor.R, seatsColor.G, seatsColor.B)),
                    BorderBrush         = new SolidColorBrush(Color.FromArgb(80,
                                              seatsColor.R, seatsColor.G, seatsColor.B)),
                    BorderThickness     = new Thickness(1),
                    CornerRadius        = new CornerRadius(4),
                    Padding             = new Thickness(6, 3, 6, 3),
                    Margin              = new Thickness(0, 5, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Child               = new TextBlock
                    {
                        Text       = seatsText,
                        FontSize   = 10,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(seatsColor)
                    }
                });
            }

            var inner = new StackPanel();
            inner.Children.Add(imgStack);
            inner.Children.Add(info);

            var card = new Border
            {
                Width           = 200,
                Height          = 310,
                Margin          = new Thickness(8),
                Background      = Brushes.White,
                BorderBrush     = new SolidColorBrush(Color.FromRgb(214, 237, 229)),
                BorderThickness = new Thickness(1),
                CornerRadius    = new CornerRadius(8),
                Cursor          = isPast ? Cursors.Arrow : Cursors.Hand,
                Child           = inner,
                Opacity         = isPast ? 0.55 : 1.0,
                Tag = item
            };

            if (!isPast)
            {
                card.MouseEnter        += (s, e) => { if (card != _selectedCard) card.BorderBrush = new SolidColorBrush(Color.FromRgb(30, 89, 67)); };
                card.MouseLeave        += (s, e) => { if (card != _selectedCard) card.BorderBrush = new SolidColorBrush(Color.FromRgb(214, 237, 229)); };
                card.MouseLeftButtonUp += Card_Click;
            }

            return (card, imgControl);
        }

        private void Card_Click(object sender, MouseButtonEventArgs e)
        {
            if (_selectedCard != null)
            {
                _selectedCard.BorderBrush     = new SolidColorBrush(Color.FromRgb(214, 237, 229));
                _selectedCard.BorderThickness = new Thickness(1);
                _selectedCard.Background      = Brushes.White;
            }

            _selectedCard = sender as Border;
            if (_selectedCard == null) return;

            _selectedCard.BorderBrush     = new SolidColorBrush(Color.FromRgb(30, 89, 67));
            _selectedCard.BorderThickness = new Thickness(2);
            _selectedCard.Background      = new SolidColorBrush(Color.FromRgb(212, 235, 225));
            _selectedEvent = _selectedCard.Tag as IDictionary<string, object>;
        }

        private BitmapImage LoadBitmapBackground(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            string fullPath = AppSettings.GetImageFullPath(fileName);
            if (!File.Exists(fullPath)) return null;
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource        = new Uri(fullPath);
                bmp.CacheOption      = BitmapCacheOption.OnLoad;
                bmp.DecodePixelWidth = 220;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch { return null; }
        }

        private static SolidColorBrush TypeColor(string type) => type?.ToLower() switch
        {
            var t when t != null && t.Contains("образ") => new SolidColorBrush(Color.FromRgb(30,  89,  67)),
            var t when t != null && t.Contains("спорт") => new SolidColorBrush(Color.FromRgb(24, 116, 170)),
            var t when t != null && t.Contains("культ") => new SolidColorBrush(Color.FromRgb(122, 92, 191)),
            var t when t != null && t.Contains("соц")   => new SolidColorBrush(Color.FromRgb(210, 100,  30)),
            var t when t != null && t.Contains("служ")  => new SolidColorBrush(Color.FromRgb(100, 110, 120)),
            _                                            => new SolidColorBrush(Color.FromRgb(44,  62,  80))
        };

        // ── Фильтр по типу (чипы) ────────────────────────────────────────────

        private async void Chip_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Primitives.ToggleButton clicked) return;

            // Снимаем все чипы, отмечаем только нажатый
            foreach (var chip in new[] { chipAll, chipEdu, chipSport, chipCult, chipSoc })
                chip.IsChecked = chip == clicked;

            _typeFilter = clicked.Tag?.ToString() ?? "";
            await LoadEventsAsync(tbSearchEvent.Text.Trim());
        }

        // ── Поиск ────────────────────────────────────────────────────────────

        private void tbSearchEvent_TextChanged(object sender, TextChangedEventArgs e)
            => _debounce.Trigger();

        private async void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            tbSearchEvent.Text = "";
            await LoadEventsAsync();
        }

        // ── Записаться ───────────────────────────────────────────────────────

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            if (_studentCod == -1)
            {
                MessageBox.Show("Ваш профиль участника не найден. Обратитесь к администратору.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_selectedEvent == null)
            {
                MessageBox.Show("Выберите мероприятие (нажмите на карточку).",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string eventName = _selectedEvent["event_name"].ToString();
            short  eventCod  = Convert.ToInt16(_selectedEvent["event_cod"]);

            if (_selectedEvent.ContainsKey("max_participants") &&
                _selectedEvent["max_participants"] is not DBNull &&
                _selectedEvent["max_participants"] != null)
            {
                if (int.TryParse(_selectedEvent["max_participants"].ToString(), out int maxP) && maxP > 0)
                {
                    var allPart = DatabaseHelper.ExecProc("sp_Participation_GetAll");
                    int registered = 0;
                    foreach (DataRow pr in allPart.Rows)
                        if (Convert.ToInt16(pr["event_cod"]) == eventCod) registered++;
                    if (registered >= maxP)
                    {
                        MessageBox.Show(
                            $"Места на мероприятие «{eventName}» закончились.\n" +
                            $"Максимально допустимо: {maxP} участников.",
                            "Мест нет", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
            }

            var check = DatabaseHelper.ExecProc("sp_Participation_GetAll");
            foreach (DataRow row in check.Rows)
            {
                if (Convert.ToInt16(row["event_cod"])   == eventCod &&
                    Convert.ToInt32(row["student_cod"]) == _studentCod)
                {
                    MessageBox.Show($"Вы уже записаны на мероприятие «{eventName}».",
                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }

            if (MessageBox.Show($"Записаться на мероприятие:\n«{eventName}»?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes) return;

            bool ok = DatabaseHelper.ExecProcNonQuery("sp_Participation_Insert", new[]
            {
                new SqlParameter("@event_cod",   eventCod),
                new SqlParameter("@student_cod", _studentCod)
            });

            if (ok)
            {
                MessageBox.Show($"Вы успешно записались на мероприятие «{eventName}»!",
                    "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                _ = LoadMyEventsAsync();
            }
        }

        // ── Мои записи ───────────────────────────────────────────────────────

        private static DateTime GetSchoolYearStart()
        {
            var today = DateTime.Today;
            int year  = today.Month >= 9 ? today.Year : today.Year - 1;
            return new DateTime(year, 9, 1);
        }

        private async Task LoadMyEventsAsync()
        {
            if (_studentCod == -1) return;

            short myCod           = _studentCod;
            var   schoolYearStart = GetSchoolYearStart();
            var   today           = DateTime.Today;

            var (upcoming, past) = await Task.Run(() =>
            {
                var dt       = DatabaseHelper.ExecProc("sp_Participation_GetAll");
                var upList   = new List<dynamic>();
                var pastList = new List<dynamic>();
                int iUp = 1, iPast = 1;

                foreach (DataRow row in dt.Rows)
                {
                    if (Convert.ToInt32(row["student_cod"]) != myCod) continue;

                    if (row["date_e"] is DateTime evDate && evDate.Date < schoolYearStart)
                        continue;

                    var item = new ExpandoObject() as IDictionary<string, object>;
                    foreach (DataColumn col in dt.Columns)
                        item[col.ColumnName] = row[col];

                    if (row["date_e"] is DateTime d && d.Date < today)
                    {
                        item["RowNumber"] = iPast++;
                        pastList.Add((ExpandoObject)item);
                    }
                    else
                    {
                        item["RowNumber"] = iUp++;
                        upList.Add((ExpandoObject)item);
                    }
                }
                return (upList, pastList);
            });

            dgMyEvents.ItemsSource     = upcoming;
            dgMyPastEvents.ItemsSource = past;

            tbMyCount.Text     = upcoming.Count > 0
                ? $"Предстоящих: {upcoming.Count}"
                : "Нет предстоящих записей";
            tbMyPastCount.Text = past.Count > 0
                ? $"Прошедших в этом учебном году: {past.Count}"
                : "Нет прошедших мероприятий";
        }

        private void Unregister_Click(object sender, RoutedEventArgs e)
        {
            if (dgMyEvents.SelectedItem is not IDictionary<string, object> row)
            {
                MessageBox.Show("Выберите мероприятие для отмены записи.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string eventName = row["event_name"].ToString();
            short  eventCod  = Convert.ToInt16(row["event_cod"]);

            if (MessageBox.Show($"Отменить запись на мероприятие:\n«{eventName}»?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                != MessageBoxResult.Yes) return;

            bool ok = DatabaseHelper.ExecProcNonQuery("sp_Participation_Delete", new[]
            {
                new SqlParameter("@event_cod",   eventCod),
                new SqlParameter("@student_cod", _studentCod)
            });

            if (ok)
            {
                MessageBox.Show("Запись отменена.", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                _ = LoadMyEventsAsync();
            }
        }

        // ── Выход ────────────────────────────────────────────────────────────

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Выйти из системы?", "Выход",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            new LoginWindow().Show();
            Close();
        }
    }
}
