using System;
using System.IO;
using System.Windows;

namespace SchoolEventApp
{
    public partial class PrintPreviewWindow : Window
    {
        private string _htmlPath;

        public PrintPreviewWindow()
        {
            InitializeComponent();
        }

        public void LoadHtml(string htmlPath)
        {
            _htmlPath = htmlPath;
            webBrowser.Navigate(new Uri(htmlPath));
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                webBrowser.InvokeScript("execScript",
                    new object[] { "window.print();", "JavaScript" });
            }
            catch
            {
                try { webBrowser.InvokeScript("print"); }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка печати:\n" + ex.Message, "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}