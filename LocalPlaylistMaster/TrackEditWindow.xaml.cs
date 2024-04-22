using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using LocalPlaylistMaster.Backend;
using System.Windows.Media;
using System.IO;
using LocalPlaylistMaster.Backend.Utilities;

namespace LocalPlaylistMaster
{
    /// <summary>
    /// Interaction logic for TrackEditWindow.xaml
    /// </summary>
    public partial class TrackEditWindow : Window, INotifyPropertyChanged
    {
        private readonly DatabaseManager manager;
        private readonly Track track;
        private readonly DependencyProcessManager processes;

        private bool changedVolume = false;

        private double volume = 1.0;
        public double Volume
        {
            get => volume;
            set
            {
                volume = value;
                trackPlayer.mediaElement.Volume = 0.5 * (value - 1) + 0.5;
                OnPropertyChanged(nameof(Volume));
                changedVolume = true;
            }
        }

        private TimeSpan startTime;
        public TimeSpan StartTime
        {
            get => startTime;
            set
            {
                startTime = value;
                OnPropertyChanged(nameof(StartTime));
                trackPlayer.SetMarkerTime(START_MARKER, value);
            }
        }

        private TimeSpan endTime;
        public TimeSpan EndTime
        {
            get => endTime;
            set
            {
                endTime = value;
                OnPropertyChanged(nameof(EndTime));
                trackPlayer.SetMarkerTime(END_MARKER, value);
            }
        }

        public ICommand ResetStartTimeCommand { get; }
        public ICommand ResetEndTimeCommand { get; }
        public ICommand ResetVolumeCommand { get; }

        private const string START_MARKER = "start";
        public const string END_MARKER = "end";

        public TrackEditWindow(Track track, DatabaseManager manager, DependencyProcessManager processes)
        {
            InitializeComponent();
            DataContext = this;
            this.manager = manager;
            this.track = track;
            this.processes = processes;
            trackPlayer.Db = manager;
            trackPlayer.ChangeTrack(track);
            Title = $"Edit Track ({track.Name})";

            startTime = TimeSpan.Zero;
            endTime = TimeSpan.FromSeconds(track.TimeInSeconds);

            var tempStartTime = startTime;
            var tempEndTime = endTime;
            ResetStartTimeCommand = new RelayCommand(() => StartTime = tempStartTime);
            ResetEndTimeCommand = new RelayCommand(() => EndTime = tempEndTime);
            ResetVolumeCommand = new RelayCommand(() => Volume = 1);

            trackPlayer.CreateMarker(START_MARKER, Brushes.LightGreen, startTime, (t) =>
            {
                startTime = t;
                OnPropertyChanged(nameof(StartTime));
            });
            trackPlayer.CreateMarker(END_MARKER, Brushes.LightPink, endTime, (t) =>
            {
                endTime = t;
                OnPropertyChanged(nameof(EndTime));
            });
            OnPropertyChanged(nameof(StartTime));
            OnPropertyChanged(nameof(EndTime));

            trackPlayer.RedrawMarkers();
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Apply()
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to modify. This can't be undone unless you redownload.", "", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            _ = ApplyTask();
            IsEnabled = false;
        }

        private async Task ApplyTask()
        {
            FileInfo sourceFile = manager.GetTrackAudio(track);
            FileInfo tempFile = new(Path.Combine(sourceFile.Directory?.FullName ?? throw new Exception(), "temp.mp3"));

            var ffmpeg = processes.CreateFfmpegProcess();
            ffmpeg.StartInfo.CreateNoWindow = true;

            string args = $"-y -ss {StartTime} -to {EndTime} -i \"{sourceFile.FullName}\" ";
            if (changedVolume)
            {
                args += $"-filter \"volume={Volume}\" ";
            }
            else
            {
                args += "-c copy ";
            }
            args += $"\"{tempFile.FullName}\"";
            ffmpeg.StartInfo.Arguments = args;
            ffmpeg.Start();
            await ffmpeg.WaitForExitAsync();

            var backupDir = Directory.CreateDirectory(Path.Combine(sourceFile.Directory?.Parent?.FullName ?? throw new Exception(), "backup"));
            FileInfo backupFile = new(Path.Combine(backupDir.FullName, $"{track.Id}_backup.mp3"));
            File.Replace(tempFile.FullName, sourceFile.FullName, backupFile.FullName);
            var probe = new TrackProbe(sourceFile, track, processes);
            await probe.MatchDuration();
            await manager.UpdateTracks([track]);

            Dispatcher.Invoke(Close);
        }

        private void CancelButton(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ApplyButton(object sender, RoutedEventArgs e)
        {
            Apply();
        }
    }
}
