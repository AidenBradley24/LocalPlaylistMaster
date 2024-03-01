using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using LocalPlaylistMaster.Backend;

namespace LocalPlaylistMaster
{
    public class MainWindowModel : INotifyPropertyChanged
    {
        public MainWindow Host { get; init; }
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

        private bool isItemSelected;
        public bool IsItemSelected
        {
            get { return isItemSelected; }
            private set
            {
                isItemSelected = value;
                OnPropertyChanged(nameof(IsItemSelected));
            }
        }

        private int selectedCount = 0;

        public string SelectedText
        {
            get
            {
                if (IsItemSelected)
                {
                    return $"Selected ({selectedCount}) items.";
                }

                return "";
            }
        }

        #region Edit Records Properties

        CollectionPropertyManager? propertyManager;

        private bool editingTrack = true;
        public bool EditingTrack
        {
            get => editingTrack;
            set
            {
                editingTrack = value;
                OnPropertyChanged(nameof(EditingTrack));
            }
        }

        public bool EditingRemote
        {
            get => !EditingTrack;
        }

        public string EditName
        {
            get => (string)propertyManager?.GetValue(nameof(EditName)) ?? "...";
            set
            {
                propertyManager?.SetValue(nameof(EditName), value);
                OnPropertyChanged(nameof(EditName));
            }
        }

        public string EditDescription
        {
            get => (string)propertyManager?.GetValue(nameof(EditDescription)) ?? "...";
            set
            {
                propertyManager?.SetValue(nameof(EditDescription), value);
                OnPropertyChanged(nameof(EditDescription));
            }
        }

        public string EditLink
        {
            get => (string)propertyManager?.GetValue(nameof(EditDescription)) ?? "...";
            set
            {
                propertyManager?.SetValue(nameof(EditLink), value);
                OnPropertyChanged(nameof(EditLink));
            }
        }


        public bool? EditLocked
        {
            get => (bool?)(propertyManager?.GetValue(nameof(EditLocked)));
            set
            {
                propertyManager?.SetValue(nameof(EditLocked), value);
                OnPropertyChanged(nameof(EditLocked));
            }
        }

        #endregion

        // Implement commands for navigating pages
        public ICommand PreviousPageTrackCommand { get; }
        public ICommand NextPageTrackCommand { get; }

        // Constructor
        public MainWindowModel(MainWindow host)
        {
            Host = host;
            PreviousPageTrackCommand = new RelayCommand(PreviousPageTrack, CanNavigatePreviousTrack);
            NextPageTrackCommand = new RelayCommand(NextPageTrack, CanNavigateNextTrack);
            tracks = [];
        }

        // Method to load initial tracks
        public void RefreshTracks()
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
            Host.trackGrid.SelectedItems.Clear();
            OnPropertyChanged(nameof(Tracks));
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

        private bool ignoreSelection = false;
        public void DisplaySelection<T>(IEnumerable<T> items)
        {
            if (ignoreSelection) return;

            if (propertyManager?.PendingChanges ?? false)
            {
                var result = MessageBox.Show("There are pending changes. Do you want to disgard?", "Confirm", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                {
                    ClearSelection();
                    IsItemSelected = true;
                    foreach (var item in propertyManager.GetCollection<Track>())
                    {
                        Host.trackGrid.SelectedItems.Add(item);
                    }

                    Host.trackGrid.InvalidateVisual();
                    return;
                }
            }

            if (items is IEnumerable<Track> tracks) DisplayTracksEdit(tracks);
            else if (items is IEnumerable<Remote> remotes) DisplayRemotesEdit(remotes);

            selectedCount = items.Count();
            OnPropertyChanged(nameof(SelectedText));
        }

        /// <summary>
        /// Clears UI selection without changing the property edit manager
        /// </summary>
        public void ClearSelection()
        {
            ignoreSelection = true;
            if (EditingTrack)
            {
                Host.trackGrid.UnselectAll();
            }
            else if (EditingRemote)
            {
                // TODO
            }
            IsItemSelected = false;
            ignoreSelection = false;
        }

        private void DisplayTracksEdit(IEnumerable<Track> trackSelection)
        {
            if(!trackSelection.Any())
            {
                IsItemSelected = false;
                return;
            }

            IsItemSelected = true;

            string[] EDIT_NAMES = [nameof(EditName), nameof(EditDescription), nameof(EditLocked)];
            string[] ACTUAL_NAMES = [nameof(Track.Name), nameof(Track.Description), nameof(Track.Locked)];

            propertyManager = new(typeof(Track), trackSelection, EDIT_NAMES, ACTUAL_NAMES);
            foreach (string edit in EDIT_NAMES)
            {
                OnPropertyChanged(edit);
            }
        }

        private void DisplayRemotesEdit(IEnumerable<Remote> remoteSelection)
        {
            if (!remoteSelection.Any())
            {
                IsItemSelected = false;
                return;
            }

            IsItemSelected = true;

            // TODO
        }

        public void CancelItemUpdate()
        {
            if (propertyManager?.MyType == typeof(Track))
            {
                DisplayTracksEdit(propertyManager.GetCollection<Track>());
            }
            else
            {
                propertyManager = null;
            }
        }

        public void ConfirmItemUpdate()
        {
            if(propertyManager?.MyType == typeof(Track))
            {
                ClearSelection();
                propertyManager?.ApplyChanges();
                manager?.UpdateTracks(propertyManager?.GetCollection<Track>() ?? throw new Exception()).Wait();
                propertyManager = null;
                RefreshTracks();
            }

        }
    }
}
