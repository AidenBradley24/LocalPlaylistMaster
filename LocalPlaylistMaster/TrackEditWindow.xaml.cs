using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Input;
using LocalPlaylistMaster.Backend;
using System.Windows.Media;

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

            TrackProbe probe = new(manager.GetTrackAudio(track), track, processes);
            var startTime = TimeSpan.Zero;
            var endTime = TimeSpan.FromSeconds(probe.GetDuration());

            trackPlayer.CreateMarker(START_MARKER, Brushes.LightGreen, startTime);
            trackPlayer.CreateMarker(END_MARKER, Brushes.LightPink, endTime);
            //StartTime = startTime;
            //EndTime = endTime;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Apply()
        {
            string trackPath = manager.GetTrackAudio(track).FullName;
            var ffmpeg = processes.CreateFfmpegProcess();

            string args = $"-ss {StartTime} -to {EndTime} -i {trackPath} ";
            if (changedVolume)
            {
                args += $"-filter \"volume={Volume}\" ";
            }
            else
            {
                args += "-c copy ";
            }
            args += trackPath;
            ffmpeg.StartInfo.Arguments = args;

            ffmpeg.Start();
            ffmpeg.WaitForExit();
        }
    }
}
