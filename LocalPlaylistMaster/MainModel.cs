using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LocalPlaylistMaster.Backend;
using LocalPlaylistMaster.Extensions;

namespace LocalPlaylistMaster
{
    /// <summary>
    /// Most of the frontend logic for the main window
    /// </summary>
    public class MainModel : INotifyPropertyChanged
    {
        public MainWindow Host { get; init; }
        public DatabaseManager? Manager
        {
            get => manager;
            set
            {
                manager = value;
                OnPropertyChanged(nameof(DbName));
                Host.trackPlayer.Db = value;
            }
        }
        private DatabaseManager? manager;

        public Dictionary<int, string> RemoteReference
        {
            get => ((IntStringMapConverter)Host.Resources["RemoteMap"]).ValueMap;
            set => ((IntStringMapConverter)Host.Resources["RemoteMap"]).ValueMap = value;
        }

        private UserQuery trackUserQuery;

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

        private ObservableCollection<Playlist> playlists;
        public ObservableCollection<Playlist> Playlists
        {
            get { return playlists; }
            set
            {
                playlists = value;
                OnPropertyChanged(nameof(Playlists));
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
                OnPropertyChanged(nameof(EditingTrack));
                OnPropertyChanged(nameof(EditingRemote));
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

        public string DbName
        {
            get => Manager?.DbRecord.name ?? "";
        }

        private const int VIEW_SIZE = 50;
        private int currentTrackOffset = 0;
        private int currentRemoteOffset = 0;
        private int currentPlaylistOffset = 0;
        private int filteredTrackCount = 0;
        private const int MAX_RECENT = 5;

        public bool HasRecent
        {
            get => Settings.Default.RecentDbs.Count > 0;
        }

        private bool hasNotification = false;
        public bool HasNotification
        {
            get => hasNotification;
            set
            {
                hasNotification = value;
                OnPropertyChanged(nameof(HasNotification));
            }
        }

        private string notificationBackgroundColor = "green";
        public string NotificationBackgroundColor
        {
            get => notificationBackgroundColor;
            set
            {
                notificationBackgroundColor = value;
                OnPropertyChanged(nameof(NotificationBackgroundColor));
            }
        }

        private string notificationText = "";
        public string NotificationText
        {
            get => notificationText;
            set
            {
                notificationText = value;
                OnPropertyChanged(nameof(NotificationText));
            }
        }

        private int currentNotificationPriority = 0;

        private bool canRemove = false;
        public bool CanRemove
        {
            get => canRemove;
            set
            {
                canRemove = value;
                OnPropertyChanged(nameof(CanRemove));
                OnPropertyChanged(nameof(CanUndoRemove));
            }
        }

        public bool CanUndoRemove
        {
            get => !canRemove;
            set
            {
                canRemove = !value;
                OnPropertyChanged(nameof(CanRemove));
                OnPropertyChanged(nameof(CanUndoRemove));
            }
        }

        public bool CanNext
        {
            get => CanNavigateNext();
        }

        public bool CanPrevious
        {
            get => CanNavigatePrevious();
        }

        #region Edit Records Properties

        CollectionPropertyManager? propertyManager;

        private enum EditType { None, Track, Remote, Playlist }
        private EditType currentEdit;

        private void OnEditTypeChanged()
        {
            OnPropertyChanged(nameof(EditingTrack));
            OnPropertyChanged(nameof(EditingRemote));
            OnPropertyChanged(nameof(EditingPlaylist));
            OnPropertyChanged(nameof(EditingTrackOrRemote));
        }

        public bool EditingTrack
        {
            get => currentEdit == EditType.Track && IsItemSelected;
            set
            {
                currentEdit = EditType.Track;
                OnEditTypeChanged();
            }
        }

        public bool EditingRemote
        {
            get => currentEdit == EditType.Remote && IsItemSelected;
            set
            {
                currentEdit = EditType.Remote;
                OnEditTypeChanged();
            }
        }

        public bool EditingTrackOrRemote
        {
            get => EditingTrack || EditingRemote;
        }

        public bool EditingPlaylist
        {
            get => currentEdit == EditType.Playlist && IsItemSelected;
            set
            {
                currentEdit = EditType.Playlist;
                OnEditTypeChanged();
            }
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
            get => (string?)propertyManager?.GetValue(nameof(EditLink)) ?? "...";
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

        public int? EditRating
        {
            get => (int?)(propertyManager?.GetValue(nameof(EditRating)));
            set
            {
                propertyManager?.SetValue(nameof(EditRating), value);
                OnPropertyChanged(nameof(EditRating));
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

        public string EditPlaylistTrackFilter
        {
            get => (string?)(propertyManager?.GetValue(nameof(EditPlaylistTrackFilter))) ?? "...";
            set
            {
                propertyManager?.SetValue(nameof(EditPlaylistTrackFilter), value);
                OnPropertyChanged(nameof(EditPlaylistTrackFilter));
            }
        }

        #endregion

        public ICommand NewRemoteCommand { get; }
        public ICommand NewPlaylistCommand { get; }

        public ICommand RemoveTrackSelectionFromDbCommand { get; }
        public ICommand RemoveRemoteSelectionFromDbCommand { get; }
        public ICommand RemovePlaylistSelectionFromDbCommand { get; }
        public ICommand UndoRemoveTrackCommand { get; }

        public ICommand FetchRemoteSelectionCommand { get; }
        public ICommand DownloadRemoteSelectionCommand { get; }
        public ICommand SyncRemoteSelectionCommand { get; }

        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand FirstPageCommand { get; }
        public ICommand LastPageCommand { get; }

        public ICommand FetchAllCommand { get; }
        public ICommand DownloadAllCommand { get; }
        public ICommand SyncNewCommand { get; }
        public ICommand DownloadSelectedTracksCommand { get; }

        public ICommand EditFilterCommand { get; }
        public ICommand ClearFilterCommand { get; }
        public ICommand ClearSelectionCommand { get; }

        public ICommand ExportSelectedPlaylistCommand { get; }
        public ICommand RollbackCommand { get; }
        public ICommand EditAudioTrackCommand { get; }

        public MainModel(MainWindow host)
        {
            Host = host;
            tracks = [];
            remotes = [];
            playlists = [];

            trackUserQuery = new UserQuery("");

            NewRemoteCommand = new RelayCommand(AddRemote, HasDb);
            NewPlaylistCommand = new RelayCommand(AddPlaylist, HasDb);

            RemoveTrackSelectionFromDbCommand = new RelayCommand(RemoveTrackSelectionFromDb,
                () => manager != null && EditingTrack);
            RemoveRemoteSelectionFromDbCommand = new RelayCommand(RemoveRemoteSelectionFromDb,
                () => manager != null && EditingRemote);
            RemovePlaylistSelectionFromDbCommand = new RelayCommand(RemotePlaylistSelectionFromDb,
                () => manager != null && EditingPlaylist);
            UndoRemoveTrackCommand = new RelayCommand(UndoRemoveTrackFromDb, 
                () => manager != null && EditingTrack);

            FetchRemoteSelectionCommand = new RelayCommand(FetchRemoteSelection,
                () => manager != null && EditingRemote);
            DownloadRemoteSelectionCommand = new RelayCommand(DownloadRemoteSelection,
                () => manager != null && EditingRemote);
            SyncRemoteSelectionCommand = new RelayCommand(SyncRemoteSelection,
                () => manager != null && EditingRemote);

            PreviousPageCommand = new RelayCommand(PreviousPage, CanNavigatePrevious);
            NextPageCommand = new RelayCommand(NextPage, CanNavigateNext);
            FirstPageCommand = new RelayCommand(FirstPage, CanNavigatePrevious);
            LastPageCommand = new RelayCommand(LastPage, CanNavigateNext);

            FetchAllCommand = new RelayCommand(FetchAll, HasDb);
            DownloadAllCommand = new RelayCommand(DownloadAll, HasDb);
            SyncNewCommand = new RelayCommand(SyncNew, HasDb);
            DownloadSelectedTracksCommand = new RelayCommand(DownloadSelectedTracks,
                () => manager != null && EditingTrack && !CanUndoRemove);

            EditFilterCommand = new RelayCommand(EditFilter, HasDb);
            ClearFilterCommand = new RelayCommand(ClearFilter, HasDb);
            ClearSelectionCommand = new RelayCommand(() =>
            {
                if (propertyManager?.PendingChanges ?? false)
                {
                    var result = MessageBox.Show("There are pending changes. Do you want to disgard?", "Confirm", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.No)
                    {
                        return;
                    }
                }
                ClearSelection();
            },
            () => manager != null && propertyManager != null);

            ExportSelectedPlaylistCommand = new RelayCommand(ExportSelectedPlaylist,
                () => manager != null && EditingPlaylist);
            RollbackCommand = new RelayCommand(Rollback, () => EditingTrack);
            EditAudioTrackCommand = new RelayCommand(EditAudioTrack, () =>
            {
                if (manager == null || !EditingTrack) return false;
                var tracks = propertyManager?.GetCollection<Track>();
                if(tracks?.Count() != 1) return false;
                if (tracks.First().Downloaded) return true;
                return false;
            });

            if (Settings.Default.RecentDbs == null) Settings.Default.RecentDbs = [];
            UpdateRecent();
        }

        internal void UpdateRecent()
        {
            int over = Settings.Default.RecentDbs.Count - MAX_RECENT;
            for (int i = 0; i < over; i++)
            {
                Settings.Default.RecentDbs.RemoveAt(0);
            }

            Host.RecentMenu.Items.Clear();
            for (int i = Settings.Default.RecentDbs.Count - 1; i >= 0; i--)
            {
                string path = Settings.Default.RecentDbs[i] ?? "";
                MenuItem childMenu = new()
                {
                    Header = Settings.Default.RecentDbs[i],
                    Command = new RelayCommand(() => Host.OpenExistingDb(path))
                };
                Host.RecentMenu.Items.Add(childMenu);
            }

            Host.RecentMenu.Items.Add(new Separator());
            MenuItem clearRecent = new()
            {
                Header = "Clear",
                Command = new RelayCommand(() => 
                {
                    MessageBoxResult result = MessageBox.Show("Clear recent?", "Confirm", MessageBoxButton.YesNo);
                    if (result != MessageBoxResult.Yes) return;
                    Settings.Default.RecentDbs.Clear();
                    OnPropertyChanged(nameof(HasRecent));
                    UpdateRecent();
                })
            };
            Host.RecentMenu.Items.Add(clearRecent);

            Settings.Default.Save();
        }

        internal void AddRecent(string recent)
        {
            if (Settings.Default.RecentDbs.Contains(recent))
            {
                Settings.Default.RecentDbs.Remove(recent);
            }

            Settings.Default.RecentDbs.Add(recent);
            UpdateRecent();
        }

        private bool HasDb() => manager != null;

        public void RefreshAll()
        {
            RefreshTracks();
            RefreshRemotes();
            RefreshPlaylists();
        }

        public void RefreshTracks()
        {
            if (Manager == null)
            {
                Tracks = [];
                return;
            }

            Host.Cursor = Cursors.Wait;

            filteredTrackCount = Manager.CountUserQuery(trackUserQuery);
            OnPropertyChanged(nameof(CanNext));
            OnPropertyChanged(nameof(CanPrevious));

            var task = Manager.ExecuteUserQuery(trackUserQuery, VIEW_SIZE, currentTrackOffset);
            task.Wait(); // TODO add loading bar
            Tracks = new ObservableCollection<Track>(task.Result);

            var remoteReferenceTask = Manager.GetRemoteNames(task.Result.Select(t => t.Remote));
            remoteReferenceTask.Wait();
            RemoteReference = remoteReferenceTask.Result;
            RemoteReference.Add(-1, "NONE");
            Host.trackGrid.SelectedItems.Clear();
            OnPropertyChanged(nameof(Tracks));
            ClearNotification();

            Host.Cursor = Cursors.Arrow;
        }

        public void RefreshRemotes()
        {
            if (Manager == null)
            {
                Remotes = [];
                return;
            }

            Host.Cursor = Cursors.Wait;

            var task = Manager.GetRemotes(VIEW_SIZE, currentRemoteOffset);
            task.Wait(); // TODO add loading bar

            Remotes = new ObservableCollection<Remote>(task.Result);
            Host.remoteGrid.SelectedItems.Clear();
            OnPropertyChanged(nameof(Remotes));
            OnPropertyChanged(nameof(CanNext));
            OnPropertyChanged(nameof(CanPrevious));

            Host.Cursor = Cursors.Arrow;
        }

        public void RefreshPlaylists()
        {
            if (Manager == null)
            {
                Playlists = [];
                return;
            }

            Host.Cursor = Cursors.Wait;

            var task = Manager.GetPlaylists(VIEW_SIZE, currentPlaylistOffset);
            task.Wait(); // TODO add loading bar

            Playlists = new ObservableCollection<Playlist>(task.Result);
            Host.playlistGrid.SelectedItem = null;
            OnPropertyChanged(nameof(Playlists));
            OnPropertyChanged(nameof(CanNext));
            OnPropertyChanged(nameof(CanPrevious));

            Host.Cursor = Cursors.Arrow;
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
                    RefreshRemotes();
                    break;
                case "playlistsTab":
                    currentPlaylistOffset -= VIEW_SIZE;
                    RefreshPlaylists();
                    break;
            }

            OnPropertyChanged(nameof(CanNext));
            OnPropertyChanged(nameof(CanPrevious));
        }

        private bool CanNavigatePrevious()
        {
            if (Manager == null) return false;

            return Host.CurrentTab.Name switch
            {
                "tracksTab" => currentTrackOffset > 0,
                "remotesTab" => currentRemoteOffset > 0,
                "playlistsTab" => currentPlaylistOffset > 0,
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
                    RefreshTracks();
                    break;
                case "playlistsTab":
                    currentPlaylistOffset += VIEW_SIZE;
                    RefreshPlaylists();
                    break;
            }

            OnPropertyChanged(nameof(CanNext));
            OnPropertyChanged(nameof(CanPrevious));
        }

        private bool CanNavigateNext()
        {
            if (Manager == null) return false;

            return Host.CurrentTab.Name switch
            {
                "tracksTab" => currentTrackOffset + VIEW_SIZE < filteredTrackCount,
                "remotesTab" => currentRemoteOffset + VIEW_SIZE < Manager.GetRemoteCount(),
                "playlistsTab" => currentPlaylistOffset + VIEW_SIZE < Manager.GetPlaylistCount(),
                _ => true,
            };
        }

        private void FirstPage()
        {
            if (Manager == null) return;

            switch (Host.CurrentTab.Name)
            {
                case "tracksTab":
                    currentTrackOffset = 0;
                    RefreshTracks();
                    break;
                case "remotesTab":
                    currentRemoteOffset = 0;
                    RefreshTracks();
                    break;
                case "playlistsTab":
                    currentPlaylistOffset = 0;
                    RefreshPlaylists();
                    break;
            }

            OnPropertyChanged(nameof(CanNext));
            OnPropertyChanged(nameof(CanPrevious));
        }

        private void LastPage()
        {
            if (Manager == null) return;

            switch (Host.CurrentTab.Name)
            {
                case "tracksTab":
                    currentTrackOffset = (filteredTrackCount / VIEW_SIZE) * VIEW_SIZE;
                    RefreshTracks();
                    break;
                case "remotesTab":
                    currentRemoteOffset = (Manager.GetRemoteCount() / VIEW_SIZE) * VIEW_SIZE;
                    RefreshTracks();
                    break;
                case "playlistsTab":
                    currentPlaylistOffset = (Manager.GetPlaylistCount() / VIEW_SIZE) * VIEW_SIZE;
                    RefreshPlaylists();
                    break;
            }

            OnPropertyChanged(nameof(CanNext));
            OnPropertyChanged(nameof(CanPrevious));
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
                    return;
                }
            }

            ClearNotification();
            Host.trackPlayer.Stop();

            if (items is IEnumerable<Track> tracks) DisplayTracksEdit(tracks);
            else if (items is IEnumerable<Remote> remotes) DisplayRemotesEdit(remotes);
            else if (items is IEnumerable<Playlist> playlists) DisplayPlaylistsEdit(playlists);

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
                Host.remoteGrid.UnselectAll();
            }
            else if (EditingPlaylist)
            {
                Host.playlistGrid.UnselectAll();
            }
            IsItemSelected = false;
            ignoreSelection = false;
            ClearNotification();
        }

        private void DisplayTracksEdit(IEnumerable<Track> trackSelection)
        {
            if(!trackSelection.Any())
            {
                IsItemSelected = false;
                return;
            }

            IsItemSelected = true;
            EditingTrack = true;

            string[] EDIT_NAMES = [nameof(EditName), nameof(EditDescription), nameof(EditLocked), nameof(EditArtists), nameof(EditAlbum), nameof(EditRating)];
            string[] ACTUAL_NAMES = [nameof(Track.Name), nameof(Track.Description), nameof(Track.Locked), nameof(Track.Artists), nameof(Track.Album), nameof(Track.Rating)];

            propertyManager = new(typeof(Track), trackSelection, EDIT_NAMES, ACTUAL_NAMES);
            foreach (string edit in EDIT_NAMES)
            {
                OnPropertyChanged(edit);
            }

            if(trackSelection.Count() == 1)
            {
                Track track = trackSelection.First();
                CanRemove = true;
                if (track.Settings.HasFlag(TrackSettings.removeMe))
                {
                    CanUndoRemove = true;
                    ShowNotification(5, "This track is marked for removal", "red");
                }
                else if (!track.Settings.HasFlag(TrackSettings.downloaded))
                {
                    ShowNotification(1, "This track is not downloaded", "orange");
                }
            }
            else foreach (Track track in trackSelection)
            {
                if (track.Settings.HasFlag(TrackSettings.removeMe))
                {
                    ShowNotification(5, "One or more selected tracks are marked for removal", "red");
                }
                else if (!track.Settings.HasFlag(TrackSettings.downloaded))
                {
                    ShowNotification(1, "One or more selected tracks are not downloaded", "orange");
                }
            }

            if (trackSelection.Count() == 1)
            {
                var track = trackSelection.First();
                if (track.Downloaded)
                {
                    Host.trackPlayer.Visibility = Visibility.Visible;
                    Host.trackPlayer.ChangeTrack(track);
                }
                else
                {
                    Host.trackPlayer.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                Host.trackPlayer.Visibility = Visibility.Collapsed;
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
            EditingRemote = true;

            string[] EDIT_NAMES = [nameof(EditName), nameof(EditDescription), nameof(EditLink), nameof(EditLocked)];
            string[] ACTUAL_NAMES = [nameof(Remote.Name), nameof(Remote.Description), nameof(Remote.Link), nameof(Remote.Locked)];

            propertyManager = new(typeof(Remote), remoteSelection, EDIT_NAMES, ACTUAL_NAMES);
            foreach (string edit in EDIT_NAMES)
            {
                OnPropertyChanged(edit);
            }
        }

        private void DisplayPlaylistsEdit(IEnumerable<Playlist> playlistSelection)
        {
            if (!playlistSelection.Any())
            {
                IsItemSelected = false;
                return;
            }

            IsItemSelected = true;
            EditingPlaylist = true;

            string[] EDIT_NAMES = [nameof(EditName), nameof(EditDescription), nameof(EditPlaylistTrackFilter)];
            string[] ACTUAL_NAMES = [nameof(Playlist.Name), nameof(Playlist.Description), nameof(Playlist.Tracks)];

            propertyManager = new(typeof(Playlist), playlistSelection, EDIT_NAMES, ACTUAL_NAMES);
            foreach (string edit in EDIT_NAMES)
            {
                OnPropertyChanged(edit);
            }
        }

        public void CancelItemUpdate()
        {
            if (propertyManager?.MyType == typeof(Track))
            {
                DisplayTracksEdit(propertyManager.GetCollection<Track>());
            }
            else if(propertyManager?.MyType == typeof(Remote))
            {
                DisplayRemotesEdit(propertyManager.GetCollection<Remote>());
            }
            else if(propertyManager?.MyType == typeof(Playlist))
            {
                DisplayPlaylistsEdit(propertyManager.GetCollection<Playlist>());
            }
        }

        public void ConfirmItemUpdate()
        {
            if(propertyManager?.MyType == typeof(Track))
            {
                ClearSelection();
                propertyManager?.ApplyChanges();
                Manager?.UpdateTracks(propertyManager?.GetCollection<Track>() ?? throw new Exception()).Wait();
                propertyManager = null;
                RefreshTracks();
            }
            else if(propertyManager?.MyType == typeof(Remote))
            {
                ClearSelection();
                propertyManager?.ApplyChanges();
                Manager?.UpdateRemotes(propertyManager?.GetCollection<Remote>() ?? throw new Exception()).Wait();
                propertyManager = null;
                RefreshRemotes();
            }
            else if(propertyManager?.MyType == typeof(Playlist))
            {
                ClearSelection();
                propertyManager?.ApplyChanges();
                Manager?.UpdatePlaylists(propertyManager?.GetCollection<Playlist>() ?? throw new Exception()).Wait();
                propertyManager = null;
                RefreshPlaylists();
            }
        }

        private void Rollback()
        {
            AssertDb();
            if (propertyManager == null || propertyManager.MyType != typeof(Track)) return;
            if (HasPendingChanges()) return;
            var tracks = propertyManager.GetCollection<Track>();

            if(tracks.Count() == 1)
            {
                Track track = tracks.First();
                var soft = track.SoftRollback();
                if (soft == null) return;
                var (name, description, artists, album, rating) = soft.Value;
                EditName = name;
                EditDescription = description;
                EditArtists = artists;
                EditAlbum = album;
                EditRating = rating;
                return;
            }

            var result = MessageBox.Show("Do you want to rollback tracks to fetched data?\nThis cannot be undone.", "Rollback", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
            foreach(Track track in tracks)
            {
                track.Rollback();
            }
            var task = manager?.UpdateTracks(tracks) ?? throw new Exception();
            task.Wait();
            RefreshTracks();           
        }

        #region Db Updates

        private DatabaseManager AssertDb()
        {
            if (manager == null)
            {
                MessageBox.Show("No database is open.\nCreate a playlist first.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                throw new InvalidOperationException("Database must be open!");
            }
            return manager;
        }

        private bool HasPendingChanges()
        {
            if (propertyManager?.PendingChanges ?? false)
            {
                MessageBox.Show("You have unapplied changes.\nCancel or apply them to continue.", "Not allowed", MessageBoxButton.OK, MessageBoxImage.Error);
                return true;
            }
            return false;
        }

        private delegate Task TrackedFunction(IProgress<(ProgressModel.ReportType, object report)> reporter);

        private async Task TrackedTask(TrackedFunction function)
        {
            ProgressModel progressModel = new();
            ProgressDisplay progressDisplayWindow = new(progressModel);
            var reporter = progressModel.GetProgressReporter();
            progressDisplayWindow.Show();
            Host.IsEnabled = false;
            await function.Invoke(reporter);
            Host.IsEnabled = true;
            progressDisplayWindow.Close();
        }

        /// <summary>
        /// Download tracks if not already downloaded
        /// </summary>
        public async void DownloadSelectedTracks()
        {
            var manager = AssertDb();
            if (propertyManager?.MyType != typeof(Track)) return;
            if (HasPendingChanges()) return;

            IEnumerable<Track> tracks = propertyManager.GetCollection<Track>();
            tracks = tracks.Where(x => !x.Downloaded);

            await TrackedTask(async (reporter) =>
            {
                await manager.DownloadTracks(tracks, reporter);
            });

            RefreshTracks();
        }

        public async void FetchAll()
        {
            var manager = AssertDb();
            if (HasPendingChanges()) return;

            await TrackedTask(async (reporter) =>
            {
                int currentOffset = 0;
                while (currentOffset < manager.GetRemoteCount())
                {
                    var remotes = await manager.GetRemotes(VIEW_SIZE, currentOffset);
                    foreach (var remote in remotes)
                    {
                        await manager.FetchRemote(remote.Id, reporter);
                    }
                    currentOffset += VIEW_SIZE;
                }
            });

            RefreshAll();
        }

        public async void DownloadAll()
        {
            var manager = AssertDb();
            if (HasPendingChanges()) return;

            await TrackedTask(async (reporter) =>
            {
                int currentOffset = 0;
                while (currentOffset < manager.GetTrackCount())
                {
                    var tracks = await manager.GetTracks(VIEW_SIZE, currentOffset);
                    await manager.DownloadTracks(tracks, reporter);
                    currentOffset += VIEW_SIZE;
                }
            });

            RefreshAll();
        }

        public async void SyncNew()
        {
            var manager = AssertDb();
            if (HasPendingChanges()) return;

            await TrackedTask(async (reporter) =>
            {
                int currentOffset = 0;
                while (currentOffset < manager.GetRemoteCount())
                {
                    var remotes = await manager.GetRemotes(VIEW_SIZE, currentOffset);
                    foreach (var remote in remotes)
                    {
                        await manager.SyncRemote(remote.Id, reporter);
                    }
                    currentOffset += VIEW_SIZE;
                }
            });

            RefreshAll();
        }

        public void AddRemote()
        {
            var manager = AssertDb();
            NewRemoteWindow window = new(manager)
            {
                Topmost = true,
                Owner = Host,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            window.ShowDialog();
            RefreshAll();
        }

        public void AddPlaylist()
        {
            var manager = AssertDb();
            NewPlaylistWindow window = new(manager)
            {
                Topmost = true,
                Owner = Host,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            window.ShowDialog();
            RefreshAll();
        }

        public async void RemoveTrackSelectionFromDb()
        {
            var manager = AssertDb();
            if (propertyManager?.MyType != typeof(Track)) return;
            if (HasPendingChanges()) return;

            var result = MessageBox.Show("Are you sure that you want to remove selected tracks?\n" +
                $"{string.Join('\n', propertyManager.GetCollection<Track>().Select(t => $"#{t.Id} -- `{t.Name}`"))}",
                "Remove Tracks", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel) return;

            await TrackedTask(async (reporter) =>
            {
                reporter.Report((ProgressModel.ReportType.TitleText, "Deleting tracks"));
                int i = 0;
                IEnumerable<Track> tracks = propertyManager.GetCollection<Track>();
                foreach (Track track in tracks)
                {
                    reporter.Report((ProgressModel.ReportType.Progress, (int)((float)i++ / selectedCount * 100)));
                    await manager.RemoveTrack(track.Id);
                }
            });

            RefreshAll();
        }

        public async void UndoRemoveTrackFromDb()
        {
            var manager = AssertDb();
            if (propertyManager?.MyType != typeof(Track)) return;
            if (HasPendingChanges()) return;

            await TrackedTask(async (reporter) =>
            {
                reporter.Report((ProgressModel.ReportType.TitleText, "Undo deleting tracks"));
                int i = 0;
                IEnumerable<Track> tracks = propertyManager.GetCollection<Track>();
                foreach (Track track in tracks)
                {
                    reporter.Report((ProgressModel.ReportType.Progress, (int)((float)i++ / selectedCount * 100)));
                    await manager.UndoRemoveTrack(track.Id);
                }
            });

            RefreshAll();
        }

        public async void RemoveRemoteSelectionFromDb()
        {
            var manager = AssertDb();
            if (propertyManager?.MyType != typeof(Remote)) return;
            if (HasPendingChanges()) return;

            var result = MessageBox.Show("Are you sure that you want to remove selected remotes?\n" +
                $"{string.Join('\n', propertyManager.GetCollection<Remote>().Select(r => $"#{r.Id} -- `{r.Name}`"))}",
                "Remove Remotes", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel) return;

            result = MessageBox.Show("Do you also want to delete associated tracks? -- This can't be undone", "Remove Remotes", MessageBoxButton.YesNoCancel);
            if (result == MessageBoxResult.Cancel) return;
            bool alsoDeleteTracks = result == MessageBoxResult.Yes;

            await TrackedTask(async (reporter) =>
            {
                reporter.Report((ProgressModel.ReportType.TitleText, "Deleting remotes"));
                int i = 0;
                IEnumerable<Remote> remotes = propertyManager.GetCollection<Remote>();
                foreach (Remote remote in remotes)
                {
                    reporter.Report((ProgressModel.ReportType.Progress, (int)((float)i++ / selectedCount * 100)));
                    await manager.RemoveRemote(remote.Id, alsoDeleteTracks);
                }
            });

            RefreshAll();
        }

        public async void RemotePlaylistSelectionFromDb()
        {
            var manager = AssertDb();
            if (propertyManager?.MyType != typeof(Playlist)) return;
            if (HasPendingChanges()) return;

            var result = MessageBox.Show("Are you sure that you want to remove selected playlists?\n" +
                $"{string.Join('\n', propertyManager.GetCollection<Playlist>().Select(p => $"#{p.Id} -- `{p.Name}`"))}",
                "Remove Playlists", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel) return;

            await TrackedTask(async (reporter) =>
            {
                reporter.Report((ProgressModel.ReportType.TitleText, "Deleting playlists"));
                int i = 0;
                IEnumerable<Playlist> playlists = propertyManager.GetCollection<Playlist>();
                foreach (Playlist playlist in playlists)
                {
                    reporter.Report((ProgressModel.ReportType.Progress, (int)((float)i++ / selectedCount * 100)));
                    await manager.RemovePlaylist(playlist.Id);
                }
            });

            RefreshAll();
        }

        public async void FetchRemoteSelection()
        {
            var manager = AssertDb();
            if (propertyManager?.MyType != typeof(Remote)) return;
            if (HasPendingChanges()) return;

            await TrackedTask(async (reporter) =>
            {
                foreach (var remote in propertyManager.GetCollection<Remote>())
                {
                    await manager.FetchRemote(remote.Id, reporter);
                }
            });

            RefreshAll();
        }

        public async void DownloadRemoteSelection()
        {
            var manager = AssertDb();
            if (propertyManager?.MyType != typeof(Remote)) return;
            if (HasPendingChanges()) return;

            await TrackedTask(async (reporter) =>
            {
                foreach (var remote in propertyManager.GetCollection<Remote>())
                {
                    await manager.DownloadTracks(remote.Id, reporter);
                }
            });

            RefreshAll();
        }

        public async void SyncRemoteSelection()
        {
            var manager = AssertDb();
            if (propertyManager?.MyType != typeof(Remote)) return;
            if (HasPendingChanges()) return;

            await TrackedTask(async (reporter) =>
            {
                foreach (var remote in propertyManager.GetCollection<Remote>())
                {
                    await manager.SyncRemote(remote.Id, reporter);
                }
            });

            RefreshAll();
        }

        public void EditFilter()
        {
            var manager = AssertDb();
            if (HasPendingChanges()) return;
            UserQueryWindow window = new(trackUserQuery)
            {
                Owner = Host,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            bool? result = window.ShowDialog();
            if (result != true) return;
            trackUserQuery = window.Result ?? new UserQuery("");
            currentTrackOffset = 0;
            filteredTrackCount = manager.CountUserQuery(trackUserQuery);
            RefreshTracks();
        }

        public void ClearFilter()
        {
            AssertDb();
            if (HasPendingChanges()) return;

            trackUserQuery = new UserQuery("");
            currentTrackOffset = 0;
            RefreshTracks();
        }

        public void EditPlaylistTracks()
        {
            AssertDb();
            UserQuery query;

            try
            {
                query = new(EditPlaylistTrackFilter);
            }
            catch
            {
                MessageBox.Show("Error loading existing query.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            UserQueryWindow window = new(query);
            bool? result = window.ShowDialog();
            if (result != true) return;
            EditPlaylistTrackFilter = window.Result?.Query ?? "";
        }

        public void ExportSelectedPlaylist()
        {
            var manager = AssertDb();
            if (propertyManager?.MyType != typeof(Playlist)) return;
            if (HasPendingChanges()) return;

            ExportPlaylistWindow window = new(propertyManager.GetCollection<Playlist>().First(), manager);
            window.ShowDialog();
        }

        public void EditAudioTrack()
        {
            var manager = AssertDb();
            if (propertyManager?.MyType != typeof(Track)) return;
            if (HasPendingChanges()) return;
            Track track = propertyManager.GetCollection<Track>().First();
            if(!track.Downloaded) return;
            TrackEditWindow window = new(track, manager, Host.dependencyProcessManager);
            window.ShowDialog();
            RefreshTracks();
        }
        #endregion

        #region Notifications
        private void ShowNotification(int priority, string text, string color)
        {
            if(priority > currentNotificationPriority)
            {
                currentNotificationPriority = priority;
                NotificationText = text;
                NotificationBackgroundColor = color;
                HasNotification = true;
            }
        }
        private void ClearNotification()
        {
            HasNotification = false;
            currentNotificationPriority = 0;
        }
        #endregion
    }
}
