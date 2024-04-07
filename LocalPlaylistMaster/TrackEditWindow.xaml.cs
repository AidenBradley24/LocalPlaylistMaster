using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using LocalPlaylistMaster.Backend;

namespace LocalPlaylistMaster
{
    /// <summary>
    /// Interaction logic for TrackEditWindow.xaml
    /// </summary>
    public partial class TrackEditWindow : Window
    {
        private bool isPlaying = false;
        private readonly DatabaseManager manager;
        private readonly Track track;
        DispatcherTimer timer;

        public TrackEditWindow(Track track, DatabaseManager manager)
        {
            InitializeComponent();
            this.manager = manager;
            this.track = track;
            mediaElement.Source = new Uri(manager.GetTrackAudio(track).FullName);
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.2),
            };
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() => 
            {
                if (mediaElement.NaturalDuration.HasTimeSpan)
                {
                    timelineSlider.Value = mediaElement.Position.TotalSeconds;
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
            }
            else
            {
                mediaElement.Pause();
                timer.Stop();
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
    }
}
