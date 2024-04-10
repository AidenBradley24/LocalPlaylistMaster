using System.ComponentModel;
using System.Text;
using System.Windows;
using LocalPlaylistMaster.Backend;

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

        private double startTime;
        public double StartTime
        {
            get => startTime;
            set
            {
                startTime = value;
                OnPropertyChanged(nameof(StartTime));
            }
        }

        private double endTime;
        public double EndTime
        {
            get => endTime;
            set
            {
                endTime = value;
                OnPropertyChanged(nameof(EndTime));
            }
        }

        public TrackEditWindow(Track track, DatabaseManager manager, DependencyProcessManager processes)
        {
            InitializeComponent();
            this.manager = manager;
            this.track = track;
            this.processes = processes;
            trackPlayer.Db = manager;
            trackPlayer.ChangeTrack(track);

            TrackProbe probe = new(manager.GetTrackAudio(track), track, processes);
            StartTime = 0.0;
            EndTime = probe.GetDuration();
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

            string args = $"-ss {startTime} -to {endTime} -i {trackPath} ";
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
