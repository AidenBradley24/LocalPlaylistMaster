using LocalPlaylistMaster.Backend;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Diagnostics;
using static LocalPlaylistMaster.Extensions.StringCleaning;
using static LocalPlaylistMaster.Backend.Utilities.Timestamps;
using System.Timers;

namespace LocalPlaylistMaster
{
    /// <summary>
    /// Interaction logic for TrackPlayer.xaml
    /// </summary>
    public sealed partial class TrackPlayer : UserControl, IDisposable
    {
        private readonly System.Timers.Timer timer;
        private bool isPlaying = false;
        public DatabaseManager? Db { get; set; }
        public DependencyProcessManager? ProcessManager { get; set; }

        public delegate void TimeChangedCallback(TimeSpan time);
        public delegate void SelectionCallback();

        private readonly Dictionary<string, TimeSpan> markerTimes = [];
        private readonly Dictionary<string, TimeChangedCallback> timeCallbacks = [];
        private readonly Dictionary<string, SelectionCallback> selectionCallbacks = [];

        private Track? myTrack;
        private Process? ffplay;
        private TimeSpan currentTime;

        private bool isDragging = false;
        private Point startPosition;

        private const int TIMER_INTERVAL = 250;

        public TrackPlayer()
        {
            InitializeComponent();
            timer = new(TIMER_INTERVAL);
            timer.Elapsed += Tick;
            timer.AutoReset = true;
            togglePlayImage.Source = (ImageSource)FindResource("PlayIcon");
        }

        public void Dispose()
        {
            if (isPlaying) ffplay?.Kill();
            ffplay?.Dispose();
        }

        public void ChangeTrack(Track track)
        {
            Stop();
            if (ProcessManager == null) return;
            if (Db == null) return;
            ffplay = ProcessManager.CreateFFplayProcess();
            ffplay.StartInfo.CreateNoWindow = true;
            timelineSlider.Maximum = track.TimeInSeconds;
            durationText.Text = DisplayTime(track.Length);
            currentTimeText.Text = DisplayTime(TimeSpan.Zero);
            myTrack = track;
        }

        private void AdjustFFplay()
        {
            if (ffplay == null || Db == null || myTrack == null) return;
            ffplay.StartInfo.Arguments = $"-i \"{Db.GetTrackAudio(myTrack).FullName}\" -nodisp -ss {currentTime.TotalSeconds}";
            ffplay.Start();
            timer.Start();
        }

        private void Tick(object? sender, EventArgs e)
        {
            currentTime += TimeSpan.FromMilliseconds(TIMER_INTERVAL);
            Dispatcher.Invoke(() =>
            {
                timelineSlider.Value = currentTime.TotalSeconds;
                if (currentTime >= myTrack?.Length) Stop();
            });
        }

        private void TimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            currentTimeText.Text = DisplayTime(TimeSpan.FromSeconds(timelineSlider.Value));
        }

        private void TogglePlay(object sender, RoutedEventArgs e)
        {
            isPlaying = !isPlaying;
            if (isPlaying)
            {
                Play();
                togglePlayImage.Source = (ImageSource)FindResource("PauseIcon");
            }
            else
            {
                Pause();
                togglePlayImage.Source = (ImageSource)FindResource("PlayIcon");
            }
        }

        public void Play()
        {
            isPlaying = true;
            AdjustFFplay();
        }

        public void Pause()
        {
            isPlaying = false;
            ffplay?.Kill();
            timer.Stop();
        }

        public void Seek(double value)
        {
            currentTime = TimeSpan.FromSeconds(value);
            if (!isPlaying) return;
            timer.Stop();
            ffplay?.Kill();
            AdjustFFplay();
        }

        private void TimelineSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Point position = e.GetPosition(timelineSlider);
            double value = position.X / timelineSlider.ActualWidth * timelineSlider.Maximum;
            timelineSlider.Value = value;
            Seek(timelineSlider.Value);
        }

        public void Stop()
        {
            timer.Stop();
            if (isPlaying) ffplay?.Kill();
            togglePlayImage.Source = (ImageSource)FindResource("PlayIcon");
            isPlaying = false;
            timelineSlider.Value = 0;
            currentTime = TimeSpan.FromSeconds(0);
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
            if(myTrack == null) return TimeSpan.Zero;
            return myTrack.Length * (canvasPosition / (timelineSlider.ActualWidth - 10));
        }

        private double MarkerTimeToCanvasPosition(TimeSpan markerTime)
        {
            if (myTrack == null) return 0.0;
            return markerTime / myTrack.Length * (timelineSlider.ActualWidth - 10);
        }

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
            return currentTime;
        }

        public TimeSpan GetCurrentMediaLength()
        {
            return myTrack?.Length ?? TimeSpan.Zero;
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RedrawMarkers();
        }
    }
}
