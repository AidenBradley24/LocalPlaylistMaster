using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using LocalPlaylistMaster.Backend;

namespace LocalPlaylistMaster
{
    internal class MainWindowModel : INotifyPropertyChanged
    {
        public PlaylistManager? manager;
        private ObservableCollection<Track> tracks;
        public ObservableCollection<Track> Tracks
        {
            get { return tracks; }
            set
            {
                tracks = value;
                OnPropertyChanged(nameof(Tracks));
            }
        }

        // Implement commands for navigating pages
        public ICommand PreviousPageTrackCommand { get; }
        public ICommand NextPageTrackCommand { get; }

        // Constructor
        public MainWindowModel()
        {
            PreviousPageTrackCommand = new RelayCommand(PreviousPageTrack, CanNavigatePreviousTrack);
            NextPageTrackCommand = new RelayCommand(NextPageTrack, CanNavigateNextTrack);
            tracks = [];
        }

        // Method to load initial tracks
        private void RefreshTracks()
        {
            // Fetch tracks from the database or wherever you store them
            // For example:
            // Tracks = new ObservableCollection<Track>(YourDatabase.GetTracks(pageSize, pageNumber));
            // PageSize and PageNumber can be managed based on how you fetch data

            if (manager == null)
            {
                Tracks = [];
                return;
            }

            var task = manager.GetTracks();
            task.Wait(); // TODO add loading bar

            Tracks = new ObservableCollection<Track>(task.Result);
        }

        // Method to navigate to previous page of tracks
        private void PreviousPageTrack()
        {
            // TODO Implement logic to navigate to previous page of tracks
        }

        // Method to check if navigating to previous page of tracks is possible
        private bool CanNavigatePreviousTrack()
        {
            // TODO Implement logic to check if navigating to previous page of tracks is possible
            return true; // Return true or false based on your logic
        }

        // Method to navigate to next page of tracks
        private void NextPageTrack()
        {
            // TODO Implement logic to navigate to next page of tracks
        }

        // Method to check if navigating to next page of tracks is possible
        private bool CanNavigateNextTrack()
        {
            // TODO Implement logic to check if navigating to next page of tracks is possible
            return true; // Return true or false based on your logic
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
