using LocalPlaylistMaster.Backend;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace LocalPlaylistMaster
{
    /// <summary>
    /// Interaction logic for TrackPlayer.xaml
    /// </summary>
    public partial class TrackPlayer : UserControl
    {
        private readonly DispatcherTimer timer;
        private bool isPlaying = false;
        public DatabaseManager? Db { get; set; }

        public TrackPlayer()
        {
            InitializeComponent();
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.2),
            };
            timer.Tick += Tick;
            togglePlayImage.Source = (ImageSource)FindResource("PlayIcon");
        }

        private static string TimeString(TimeSpan time)
        {
            return time.ToString(@"hh\:mm\:ss");
        }

        public void ChangeTrack(Track track)
        {
            Stop();
            if (Db == null) return;
            mediaElement.Source = new Uri(Db.GetTrackAudio(track).FullName);
            durationText.Text = TimeString(TimeSpan.FromSeconds(track.TimeInSeconds));
            currentTimeText.Text = TimeString(TimeSpan.Zero);
        }

        private void Tick(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (mediaElement.NaturalDuration.HasTimeSpan)
                {
                    timelineSlider.Value = mediaElement.Position.TotalSeconds;
                    currentTimeText.Text = TimeString(mediaElement.Position);
                }
            });
        }

        private void TimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mediaElement.Position = TimeSpan.FromSeconds(timelineSlider.Value);
        }

        private void TogglePlay(object sender, RoutedEventArgs e)
        {
            isPlaying = !isPlaying;
            if (isPlaying)
            {
                mediaElement.Play();
                timer.Start();
                togglePlayImage.Source = (ImageSource)FindResource("PauseIcon");
            }
            else
            {
                mediaElement.Pause();
                timer.Stop();
                togglePlayImage.Source = (ImageSource)FindResource("PlayIcon");
            }
        }

        private void MediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            timelineSlider.Maximum = mediaElement.NaturalDuration.TimeSpan.TotalSeconds;
        }

        private void TimelineSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Point position = e.GetPosition(timelineSlider);
            double value = position.X / timelineSlider.ActualWidth * timelineSlider.Maximum;
            timelineSlider.Value = value;
            if (mediaElement != null && mediaElement.NaturalDuration.HasTimeSpan)
            {
                mediaElement.Position = TimeSpan.FromSeconds(value);
            }
        }

        public void Stop()
        {
            timer.Stop();
            mediaElement.Stop();
            togglePlayImage.Source = (ImageSource)FindResource("PlayIcon");
            isPlaying = false;
            timelineSlider.Value = 0;
        }
    }
}
