using LocalPlaylistMaster.Backend;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using static LocalPlaylistMaster.Extensions.StringCleaning;

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

        public delegate void TimeChangedCallback(TimeSpan time);
        public delegate void SelectionCallback();

        private readonly Dictionary<string, TimeSpan> markerTimes = [];
        private readonly Dictionary<string, TimeChangedCallback> timeCallbacks = [];
        private readonly Dictionary<string, SelectionCallback> selectionCallbacks = [];

        public TrackPlayer()
        {
            InitializeComponent();
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.2),
            };
            timer.Tick += Tick;
            togglePlayImage.Source = (ImageSource)FindResource("PlayIcon");
            mediaElement.MediaEnded += (_, _) => Stop();
        }

        private static string TimeString(TimeSpan time)
        {
            if(time > TimeSpan.FromHours(1))
            {
                return time.ToString(@"hh\:mm\:ss");
            }

            return time.ToString(@"mm\:ss");
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
                }
            });
        }

        private void TimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mediaElement.Position = TimeSpan.FromSeconds(timelineSlider.Value);
            currentTimeText.Text = TimeString(mediaElement.Position);
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

        public TimeSpan GetMarkerTime(string marker)
        {
            return markerTimes[marker];
        }

        public void SetMarkerTime(string marker, TimeSpan time)
        {
            if (!markerTimes.ContainsKey(marker)) return;
            markerTimes[marker] = time;
            UIElement element = (UIElement)Markers.FindName(marker);
            Canvas.SetLeft(element, MarkerTimeToCanvasPosition(time));
        }

        public void CreateMarker(string name, Brush fill, int order, TimeSpan initialTime, TimeChangedCallback timeCallback, SelectionCallback? selectionCallback = null)
        {
            const int TEXT_HEIGHT = 10;

            name = CleanElementName(name);
            Grid markerElement = new()
            {
                Width = 12,
                Height = 25 + order * TEXT_HEIGHT,
                Name = name,
                Background = Brushes.Transparent
            };

            if(selectionCallback != null)
            {
                selectionCallbacks.Add(name, selectionCallback);
            }

            markerElement.MouseDown += MarkerMouseDown;
            markerElement.MouseMove += MarkerMouseMove;
            markerElement.MouseUp += MarkerMouseUp;
            Rectangle rectangle = new()
            {
                Fill = fill,
                Width = 2,
                Margin = new Thickness(5, -10, 5, -8)
            };
            markerElement.Children.Add(rectangle);
            TextBlock textBlock = new()
            {
                FontSize = 5,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Height = TEXT_HEIGHT,
                Background = fill,
                Text = name,
                Margin = new Thickness(0, -18, 0, 0)
            };
            markerElement.Children.Add(textBlock);

            Markers.Children.Add(markerElement);
            Canvas.SetLeft(markerElement, MarkerTimeToCanvasPosition(initialTime));
            markerTimes.Add(name, initialTime);
            Markers.RegisterName(name, markerElement);
            timeCallbacks.Add(name, timeCallback);
            Task.Delay(250).ContinueWith(t => Dispatcher.Invoke(RedrawMarkers)); // TODO this is not a good solution
        }

        public void ClearMarkers()
        {
            string[] markerNames = [.. markerTimes.Keys];
            foreach(string name in markerNames)
            {
                RemoveMarker(name);
            }
        }

        public void RemoveMarker(string marker)
        {
            if (!markerTimes.ContainsKey(marker)) return;
            markerTimes.Remove(marker);
            UIElement childToRemove = (UIElement)Markers.FindName(marker);
            Markers.Children.Remove(childToRemove);
            Markers.UnregisterName(marker);
            timeCallbacks.Remove(marker);
            selectionCallbacks.Remove(marker);
        }

        private TimeSpan CanvasPositionToMarkerTime(double canvasPosition)
        {
            return mediaElement.NaturalDuration.TimeSpan * (canvasPosition / (timelineSlider.ActualWidth - 10));
        }

        private double MarkerTimeToCanvasPosition(TimeSpan markerTime)
        {
            if (!mediaElement.NaturalDuration.HasTimeSpan) return 0.0;
            return markerTime / mediaElement.NaturalDuration.TimeSpan * (timelineSlider.ActualWidth - 10);
        }

        private bool isDragging = false;
        private Point startPosition;

        private void MarkerMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                isDragging = true;
                ((UIElement)sender).CaptureMouse();
                startPosition = e.GetPosition(timelineSlider);
                string name = ((FrameworkElement)sender).Name;
                if (selectionCallbacks.TryGetValue(name, out var callback))
                {
                    callback.Invoke();
                }
            }
        }

        private void MarkerMouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point currentPosition = e.GetPosition(timelineSlider);
                double deltaX = currentPosition.X - startPosition.X;
                Grid marker = (Grid)sender;
                double left = Canvas.GetLeft(marker) + deltaX;
                double rightEdge = timelineSlider.ActualWidth - 10;
                double clampedLeft = Math.Clamp(left, 0, rightEdge);
                Canvas.SetLeft(marker, clampedLeft);
                startPosition = new Point(clampedLeft, currentPosition.Y);
            }
        }

        private void MarkerMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                ((UIElement)sender).ReleaseMouseCapture();
                Grid marker = (Grid)sender;
                double left = Canvas.GetLeft(marker);
                TimeSpan time = CanvasPositionToMarkerTime(left);
                string name = ((FrameworkElement)sender).Name;
                markerTimes[name] = time;
                timeCallbacks[name].Invoke(time);
            }
        }

        public void RedrawMarkers()
        {
            foreach (var child in Markers.Children)
            {
                Grid element = (Grid)child;
                TimeSpan time = markerTimes[element.Name];
                double pos = MarkerTimeToCanvasPosition(time);
                Canvas.SetLeft(element, pos);
            }
        }

        public TimeSpan GetCurrentMediaTime()
        {
            return mediaElement.Position;
        }

        public TimeSpan GetCurrentMediaLength()
        {
            return mediaElement.NaturalDuration.TimeSpan;
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RedrawMarkers();
        }
    }
}
