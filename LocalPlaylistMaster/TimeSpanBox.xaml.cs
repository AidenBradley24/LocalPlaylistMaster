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
        public static readonly DependencyProperty FormatProperty = DependencyProperty.Register(
            "Format", typeof(string), typeof(TimeSpanBox), new PropertyMetadata(@"mm\:ss"));

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

        public string Format
        {
            get => (string)GetValue(FormatProperty);
        }

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
            if (TimeSpan.TryParseExact(textBox.Text, Format, CultureInfo.InvariantCulture, out var timeSpan))
            {
                Time = timeSpan;
            }
            else
            {
                textBox.Text = Time.ToString(Format, CultureInfo.InvariantCulture);
            }
        }

        private void Update()
        {
            textBox.Text = Time.ToString(Format, CultureInfo.InvariantCulture);
        }
    }
}
