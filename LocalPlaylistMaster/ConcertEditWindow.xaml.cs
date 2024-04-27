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

        private IConcertManager? manager;
        private Concert? concert;
        private Track? concertTrack;

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

            Task.Run(Startup);



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

        private async Task Startup()
        {
            manager = await db.GetRemoteManager(remote.Id) as IConcertManager;
            if (manager == null)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Error", "Concert manager could not initialize.", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                });
                return;
            }

            await manager.Initialize();   
            concert = manager.GetConcert();
            concertTrack = (await db.ExecuteUserQuery(new($"id={concert.ConcertTrackId}"), 1, 0)).First();

            Dispatcher.Invoke(() =>
            {
                trackPlayer.ChangeTrack(concertTrack);
                trackRecords = new(concert.TrackRecords);
                OnPropertyChanged(nameof(TrackRecords));
            });
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

            Task.Run(ApplyTask);
            IsEnabled = false;
        }

        private async Task ApplyTask()
        {
            if(manager == null)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Error", "Concert manager did not initialize.", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                });
                return;
            }

            await manager.SplitAndCreate();
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
