using System;
using System.Configuration;
using System.IO;
using System.Windows;

namespace SchoolEventApp
{
    /// <summary>
    /// Централизованный доступ к настройкам приложения из App.config.
    /// </summary>
    public static class AppSettings
    {
        private static string _imagesPath;

        /// <summary>
        /// Абсолютный путь к папке с изображениями мероприятий.
        /// Читается из App.config (ключ "ImagesPath").
        /// Папка создаётся автоматически при первом обращении.
        /// </summary>
        public static string ImagesPath
        {
            get
            {
                if (_imagesPath != null) return _imagesPath;

                string configured = ConfigurationManager.AppSettings["ImagesPath"];

                // Если ключ не задан — fallback в папку рядом с .exe
                if (string.IsNullOrWhiteSpace(configured))
                {
                    configured = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory, "Images");
                }

                // Убираем конечный слеш, Path.Combine сам разберётся
                _imagesPath = configured.TrimEnd('\\', '/');

                // Создаём папку, если её нет
                try
                {
                    if (!Directory.Exists(_imagesPath))
                        Directory.CreateDirectory(_imagesPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Не удалось создать папку для изображений:\n{_imagesPath}\n\n{ex.Message}\n\n" +
                        "Проверьте настройку ImagesPath в App.config и права доступа к папке.",
                        "Ошибка настройки", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                return _imagesPath;
            }
        }

        /// <summary>
        /// Строит полный путь к файлу изображения по его имени.
        /// </summary>
        public static string GetImageFullPath(string fileName)
            => Path.Combine(ImagesPath, Path.GetFileName(fileName));
    }
}
