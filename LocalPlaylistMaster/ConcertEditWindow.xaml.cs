using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using LocalPlaylistMaster.Backend;
using System.Windows.Media;
using System.IO;
using LocalPlaylistMaster.Backend.Utilities;
using System.Collections.ObjectModel;

namespace LocalPlaylistMaster
{
    /// <summary>
    /// Interaction logic for ConcertEditWindow.xaml
    /// </summary>
    public partial class ConcertEditWindow : Window, INotifyPropertyChanged
    {
        private readonly DatabaseManager db;
        private readonly Remote remote;
        private readonly DependencyProcessManager processes;
        private readonly IConcertManager manager;
        private readonly Concert concert;
        private readonly Track concertTrack;

        private ObservableCollection<Concert.TrackRecord> trackRecords;
        public ObservableCollection<Concert.TrackRecord> TrackRecords
        {
            get { return trackRecords; }
            set
            {
                trackRecords = value;
                OnPropertyChanged(nameof(TrackRecords));
            }
        }

        public ConcertEditWindow(Remote remote, DatabaseManager db, DependencyProcessManager processes)
        {
            InitializeComponent();
            DataContext = this;
            this.db = db;
            this.remote = remote;
            this.processes = processes;
            trackPlayer.Db = db;
            Title = $"Edit Concert ({remote.Name})";

            {
                var task = db.GetRemoteManager(remote.Id);
                task.Wait();
                manager = (IConcertManager)(task.Result ?? throw new Exception());
                concert = GetConcert();
            }
            {
                UserQuery qry = new($"id={concert.ConcertTrackId}");
                var task = db.ExecuteUserQuery(qry, 1, 0);
                task.Wait();
                concertTrack = task.Result.First();
            }

            trackPlayer.ChangeTrack(concertTrack);
            trackRecords = new(concert.TrackRecords);
            OnPropertyChanged(nameof(TrackRecords));

            //startTime = TimeSpan.Zero;
            //endTime = TimeSpan.FromSeconds(concertTrack.TimeInSeconds);

            //var tempStartTime = startTime;
            //var tempEndTime = endTime;

            //trackPlayer.CreateMarker(START_MARKER, Brushes.LightGreen, startTime, (t) =>
            //{
            //    startTime = t;
            //    OnPropertyChanged(nameof(StartTime));
            //});
            //trackPlayer.CreateMarker(END_MARKER, Brushes.LightPink, endTime, (t) =>
            //{
            //    endTime = t;
            //    OnPropertyChanged(nameof(EndTime));
            //});
            //OnPropertyChanged(nameof(StartTime));
            //OnPropertyChanged(nameof(EndTime));

            //trackPlayer.RedrawMarkers();
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

        private Concert GetConcert()
        {
            return ((IMiscJsonUser)remote).GetProperty<Concert>("concert") ?? throw new Exception();
        }

        private void SetConcert(Concert concert)
        {
            ((IMiscJsonUser)remote).SetProperty("concert", concert);
        }
    }
}
