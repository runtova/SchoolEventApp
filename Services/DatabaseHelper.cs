using System;
using System.Configuration;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Windows;

namespace SchoolEventApp
{
    public static class DatabaseHelper
    {
        // Строка подключения берётся из App.config (ключ "DefaultConnection").
        // Если App.config не найден — падаем с понятным сообщением при старте.
        private static readonly string ConnectionString =
            ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString
            ?? throw new InvalidOperationException(
                "Строка подключения 'DefaultConnection' не найдена в App.config.\n" +
                "Добавьте секцию <connectionStrings> в конфигурационный файл.");

        public static SqlConnection GetConnection() => new SqlConnection(ConnectionString);

        // Выполнить хранимую процедуру и получить DataTable
        public static DataTable ExecProc(string procName, SqlParameter[] pars = null)
        {
            var dt = new DataTable();
            try
            {
                using var conn = GetConnection();
                conn.Open();
                using var cmd = new SqlCommand(procName, conn)
                {
                    CommandType    = CommandType.StoredProcedure,
                    CommandTimeout = 30
                };
                if (pars != null) cmd.Parameters.AddRange(pars);
                using var adapter = new SqlDataAdapter(cmd);
                adapter.Fill(dt);
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Ошибка базы данных:\n{ex.Message}", "Ошибка SQL",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Неожиданная ошибка:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return dt;
        }

        // Выполнить хранимую процедуру без возврата данных (Insert/Update/Delete)
        public static bool ExecProcNonQuery(string procName, SqlParameter[] pars = null)
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();
                using var cmd = new SqlCommand(procName, conn)
                {
                    CommandType    = CommandType.StoredProcedure,
                    CommandTimeout = 30
                };
                if (pars != null) cmd.Parameters.AddRange(pars);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Ошибка при сохранении данных:\n{ex.Message}", "Ошибка SQL",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Неожиданная ошибка:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public static bool TestConnection()
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось подключиться к базе данных:\n{ex.Message}",
                    "Ошибка подключения", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}
