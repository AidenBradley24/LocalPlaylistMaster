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

        private Dictionary<int, string>? remoteReference;
        public Dictionary<int, string>? RemoteReference => remoteReference;

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

        private ObservableCollection<Remote> remotes;
        public ObservableCollection<Remote> Remotes
        {
            get { return remotes; }
            set
            {
                remotes = value;
                OnPropertyChanged(nameof(Remotes));
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

        private const int VIEW_SIZE = 50;
        private int currentTrackOffset = 0;
        private int currentRemoteOffset = 0;

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
            get => (string?)propertyManager?.GetValue(nameof(EditName)) ?? "...";
            set
            {
                propertyManager?.SetValue(nameof(EditName), value);
                OnPropertyChanged(nameof(EditName));
            }
        }

        public string EditDescription
        {
            get => (string?)propertyManager?.GetValue(nameof(EditDescription)) ?? "...";
            set
            {
                propertyManager?.SetValue(nameof(EditDescription), value);
                OnPropertyChanged(nameof(EditDescription));
            }
        }

        public string EditLink
        {
            get => (string?)propertyManager?.GetValue(nameof(EditDescription)) ?? "...";
            set
            {
                propertyManager?.SetValue(nameof(EditLink), value);
                OnPropertyChanged(nameof(EditLink));
            }
        }

        public string EditAlbum
        {
            get => (string?)(propertyManager?.GetValue(nameof(EditAlbum))) ?? "...";
            set
            {
                propertyManager?.SetValue(nameof(EditAlbum), value);
                OnPropertyChanged(nameof(EditAlbum));
            }
        }

        public string EditArtists
        {
            get => (string?)(propertyManager?.GetValue(nameof(EditArtists))) ?? "...";
            set
            {
                propertyManager?.SetValue(nameof(EditArtists), value);
                OnPropertyChanged(nameof(EditArtists));
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

        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }

        public MainWindowModel(MainWindow host)
        {
            Host = host;
            PreviousPageCommand = new RelayCommand(PreviousPage, CanNavigatePrevious);
            NextPageCommand = new RelayCommand(NextPage, CanNavigateNext);
            tracks = [];
            remotes = [];
        }

        public void RefreshTracks()
        {
            if (manager == null)
            {
                Tracks = [];
                return;
            }

            var task = manager.GetTracks(VIEW_SIZE, currentTrackOffset);
            task.Wait(); // TODO add loading bar
            Tracks = new ObservableCollection<Track>(task.Result);

            var remoteReferenceTask = manager.GetRemoteNames(task.Result.Select(t => t.Remote));
            remoteReferenceTask.Wait();
            remoteReference = remoteReferenceTask.Result;

            Host.trackGrid.SelectedItems.Clear();
            OnPropertyChanged(nameof(Tracks));
        }

        public void RefreshRemotes()
        {
            if (manager == null)
            {
                Remotes = [];
                return;
            }

            var task = manager.GetRemotes(VIEW_SIZE, currentRemoteOffset);
            task.Wait(); // TODO add loading bar

            Remotes = new ObservableCollection<Remote>(task.Result);
            Host.trackGrid.SelectedItems.Clear();
            OnPropertyChanged(nameof(Remotes));
        }

        private void PreviousPage()
        {
            switch(Host.CurrentTab.Name)
            {
                case "tracksTab":
                    currentTrackOffset -= VIEW_SIZE;
                    RefreshTracks();
                    break;
                case "remotesTab":
                    currentRemoteOffset -= VIEW_SIZE;
                    break;
            }
        }

        private bool CanNavigatePrevious()
        {
            if (manager == null) return false;

            return Host.CurrentTab.Name switch
            {
                "tracksTab" => currentTrackOffset > 0,
                "remotesTab" => currentRemoteOffset > 0,
                _ => true,
            };
        }

        private void NextPage()
        {
            switch (Host.CurrentTab.Name)
            {
                case "tracksTab":
                    currentTrackOffset += VIEW_SIZE;
                    RefreshTracks();
                    break;
                case "remotesTab":
                    currentRemoteOffset += VIEW_SIZE;
                    break;
            }
        }

        private bool CanNavigateNext()
        {
            if (manager == null) return false;

            return Host.CurrentTab.Name switch
            {
                "tracksTab" => currentTrackOffset + VIEW_SIZE < manager.GetTrackCount(),
                "remotesTab" => currentRemoteOffset + VIEW_SIZE < manager.GetRemoteCount(),
                _ => true,
            };
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

            string[] EDIT_NAMES = [nameof(EditName), nameof(EditDescription), nameof(EditLocked), nameof(EditArtists), nameof(EditAlbum)];
            string[] ACTUAL_NAMES = [nameof(Track.Name), nameof(Track.Description), nameof(Track.Locked), nameof(Track.Artists), nameof(Track.Album)];

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
