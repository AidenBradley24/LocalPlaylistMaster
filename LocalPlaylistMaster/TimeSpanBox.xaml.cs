using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Globalization;

namespace LocalPlaylistMaster
{
    /// <summary>
    /// Interaction logic for TimeSpanBox.xaml
    /// </summary>
    public partial class TimeSpanBox : UserControl
    {
        public static readonly DependencyProperty TimeProperty = DependencyProperty.Register(
            "Time", typeof(TimeSpan), typeof(TimeSpanBox), new PropertyMetadata(TimeSpan.Zero, new PropertyChangedCallback(OnSpanPropertyChanged)));

        private static void OnSpanPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimeSpanBox box = (TimeSpanBox)d;
            box.Update();
        }

        public TimeSpan Time
        {
            get => (TimeSpan)GetValue(TimeProperty);
            set => SetValue(TimeProperty, value);
        }

        private const string MINUTE_FORMAT = @"mm\:ss";
        private const string HOUR_FORMAT = @"hh\:mm\:ss";

        public TimeSpanBox()
        {
            InitializeComponent();
            Update();
        }

        private void TimeSpanTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(c => char.IsDigit(c) || c == ':');
        }

        private void TimeSpanTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            FinishInput();
        }

        private void TimeSpanTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                FinishInput();
            }
        }

        private void FinishInput()
        {
            if (TimeSpan.TryParse(textBox.Text, CultureInfo.InvariantCulture, out var timeSpan))
            {
                Time = timeSpan;
            }
            else
            {
                string format = Time.TotalHours >= 1 ? HOUR_FORMAT : MINUTE_FORMAT;
                textBox.Text = Time.ToString(format, CultureInfo.InvariantCulture);
            }
        }

        private void Update()
        {
            string format = Time.TotalHours >= 1 ? HOUR_FORMAT : MINUTE_FORMAT;
            textBox.Text = Time.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
