using System;
using System.Windows.Threading;

namespace SchoolEventApp
{
    /// <summary>
    /// Помогает реализовать "debounce" для TextChanged:
    /// запрос в базу уходит только через DelayMs мс после последнего нажатия клавиши.
    /// Пример использования в code-behind:
    ///
    ///   private readonly SearchDebounce _debounce;
    ///
    ///   public MyWindow()
    ///   {
    ///       InitializeComponent();
    ///       _debounce = new SearchDebounce(350, () => LoadData(tbSearch.Text.Trim()));
    ///   }
    ///
    ///   private void tbSearch_TextChanged(object sender, TextChangedEventArgs e)
    ///       => _debounce.Trigger();
    /// </summary>
    public sealed class SearchDebounce
    {
        private readonly DispatcherTimer _timer;

        public SearchDebounce(int delayMs, Action callback)
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(delayMs)
            };
            _timer.Tick += (_, _) =>
            {
                _timer.Stop();
                callback();
            };
        }

        /// <summary>Перезапускает таймер — вызывай из TextChanged.</summary>
        public void Trigger()
        {
            _timer.Stop();
            _timer.Start();
        }
    }
}
