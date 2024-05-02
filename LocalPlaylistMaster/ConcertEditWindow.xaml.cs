using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using LocalPlaylistMaster.Backend;
using System.Windows.Media;
using System.IO;
using LocalPlaylistMaster.Backend.Utilities;
using System.Collections.ObjectModel;
using static LocalPlaylistMaster.TrackPlayer;

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

        private ObservableCollection<Concert.TrackRecord>? trackRecords;
        public ObservableCollection<Concert.TrackRecord>? TrackRecords
        {
            get { return trackRecords; }
            set
            {
                trackRecords = value;
                OnPropertyChanged(nameof(TrackRecords));
            }
        }

        public ICommand AddRowCommand { get; }
        public ICommand RemoveRowCommand { get; }
        public ICommand JumpToStartCommand { get; }
        public ICommand JumpToEndCommand { get; }

        public ConcertEditWindow(Remote remote, DatabaseManager db, DependencyProcessManager processes)
        {
            InitializeComponent();
            DataContext = this;
            this.db = db;
            this.remote = remote;
            this.processes = processes;
            trackPlayer.Db = db;
            Title = $"Edit Concert ({remote.Name})";

            AddRowCommand = new RelayCommand(AddRow, () => TrackRecords != null);
            RemoveRowCommand = new RelayCommand(RemoveRow, () => TrackRecords != null && trackGrid.SelectedItem != null);
            JumpToStartCommand = new RelayCommand(JumpToStart, () => TrackRecords != null && trackGrid.SelectedItem != null);
            JumpToEndCommand = new RelayCommand(JumpToEnd, () => TrackRecords != null && trackGrid.SelectedItem != null);

            Task.Run(Startup);     
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
                RecalcMarkers();
            });
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Apply()
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to apply. This will replace existing markers.", "", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            ProgressModel progressModel = new();
            ProgressDisplay progressDisplayWindow = new(progressModel);
            var reporter = progressModel.GetProgressReporter();
            progressDisplayWindow.Show();
            IsEnabled = false;
            Hide();

            void closeAction(Task _)
            {
                Dispatcher.Invoke(() =>
                {
                    IsEnabled = true;
                    progressDisplayWindow.Close();
                    Close();
                });
            }

            Task.Run(async () => await ApplyTask(reporter)).ContinueWith(closeAction);
        }

        private async Task ApplyTask(IProgress<(ProgressModel.ReportType type, object report)> reporter)
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

            await manager.SplitAndCreate(reporter);
        }

        private void CancelButton(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ApplyButton(object sender, RoutedEventArgs e)
        {
            Apply();
        }

        private void AddRow()
        {
            if (TrackRecords == null || trackGrid.SelectedItem == null) return;
            trackGrid.SelectedItem = null;
            var record = new Concert.TrackRecord("NEW TRACK", trackPlayer.GetCurrentMediaTime(), trackPlayer.GetCurrentMediaLength(), -1);
            TrackRecords.Add(record);
            RecalcMarkers();
            trackGrid.SelectedItem = record;
        }

        private void RemoveRow()
        {
            if (TrackRecords == null || trackGrid.SelectedItem == null) return;
            var selection = (Concert.TrackRecord)trackGrid.SelectedItem;
            trackGrid.SelectedItem = null;
            TrackRecords.Remove(selection);
            RecalcMarkers();
        }

        private void JumpToStart()
        {
            if (TrackRecords == null || trackGrid.SelectedItem == null) return;
            trackPlayer.mediaElement.Position = ((Concert.TrackRecord)trackGrid.SelectedItem).StartTime;
        }

        private void JumpToEnd()
        {
            if (TrackRecords == null || trackGrid.SelectedItem == null) return;
            trackPlayer.mediaElement.Position = ((Concert.TrackRecord)trackGrid.SelectedItem).EndTime;
        }

        private void RecalcMarkers()
        {
            if (trackRecords == null) return;
            trackGrid.SelectedItem = null;
            trackPlayer.ClearMarkers();
            int id = 0;
            foreach (var record in trackRecords)
            {
                record.TrackId = ++id;
                void selectionCallback() 
                {
                    trackGrid.SelectedItem = null;
                    trackGrid.SelectedItem = record;
                }
                trackPlayer.CreateMarker($"S_{id}", Brushes.Green, 0, record.StartTime, t => record.StartTime = t, selectionCallback);
                trackPlayer.CreateMarker($"E_{id}", Brushes.Red, 1, record.EndTime, t => record.EndTime = t, selectionCallback);
            }
        }
    }
}
