using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static LocalPlaylistMaster.Backend.Utilities.Timestamps;

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
            try
            {
                Time = ParseTime(textBox.Text);
            }
            catch
            {
                textBox.Text = DisplayTime(Time);
            }
        }

        private void Update()
        {
            textBox.Text = DisplayTime(Time);
        }
    }
}
