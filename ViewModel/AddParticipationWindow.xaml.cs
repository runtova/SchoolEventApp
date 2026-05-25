using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Dynamic;
using System.Windows;

namespace SchoolEventApp
{
    public partial class AddParticipationWindow : Window
    {
        public AddParticipationWindow() { InitializeComponent(); Loaded += OnLoaded; }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadEvents();
            LoadStudents();
        }

        private void LoadEvents()
        {
            try
            {
                var dt   = DatabaseHelper.ExecProc("sp_Event_GetAll");
                var today = DateTime.Today;
                var list = new List<dynamic>();

                foreach (DataRow r in dt.Rows)
                {
                    // ПАТЧ 5: администратор тоже не может записать на прошедшее
                    if (r["date_e"] is DateTime d && d.Date < today)
                        continue;

                    var item = new ExpandoObject() as IDictionary<string, object>;
                    item["event_cod"]   = r["event_cod"];
                    item["DisplayText"] = $"{r["event_name"]} ({Convert.ToDateTime(r["date_e"]):dd.MM.yyyy})";
                    list.Add((ExpandoObject)item);
                }
                cbEvent.ItemsSource = list;
                if (list.Count > 0) cbEvent.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки мероприятий: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadStudents()
        {
            try
            {
                var dt   = DatabaseHelper.ExecProc("sp_Participant_GetAll");
                var list = new List<dynamic>();
                foreach (DataRow r in dt.Rows)
                {
                    var item = new ExpandoObject() as IDictionary<string, object>;
                    item["student_cod"] = r["student_cod"];
                    item["DisplayText"] = $"{r["student_FIO"]} (кл. {r["class"]})";
                    list.Add((ExpandoObject)item);
                }
                cbStudent.ItemsSource = list;
                if (list.Count > 0) cbStudent.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки участников: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            tbError.Text = "";

            if (cbEvent.SelectedItem == null)
            { tbError.Text = "Выберите мероприятие."; return; }
            if (cbStudent.SelectedItem == null)
            { tbError.Text = "Выберите участника."; return; }

            short evCod = Convert.ToInt16(
                ((IDictionary<string, object>)cbEvent.SelectedItem)["event_cod"]);
            short stCod = Convert.ToInt16(
                ((IDictionary<string, object>)cbStudent.SelectedItem)["student_cod"]);

            // Проверка лимита мест
            var evDt = DatabaseHelper.ExecProc("sp_Event_GetAll");
            foreach (DataRow evRow in evDt.Rows)
            {
                if (Convert.ToInt16(evRow["event_cod"]) != evCod) continue;

                if (evDt.Columns.Contains("max_participants") &&
                    evRow["max_participants"] != DBNull.Value)
                {
                    int maxP = Convert.ToInt32(evRow["max_participants"]);
                    if (maxP > 0)
                    {
                        var partDt = DatabaseHelper.ExecProc("sp_Participation_GetAll");
                        int registered = 0;
                        foreach (DataRow pr in partDt.Rows)
                            if (Convert.ToInt16(pr["event_cod"]) == evCod) registered++;

                        if (registered >= maxP)
                        {
                            string evName = evRow["event_name"].ToString();
                            MessageBox.Show(
                                $"Невозможно добавить участника.\n" +
                                $"Мероприятие «{evName}» уже заполнено.\n" +
                                $"Максимум мест: {maxP}, записано: {registered}.",
                                "Мест нет", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }
                }
                break;
            }

            if (DatabaseHelper.ExecProcNonQuery("sp_Participation_Insert", new[]
            {
                new SqlParameter("@event_cod",   evCod),
                new SqlParameter("@student_cod", stCod)
            }))
                DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
