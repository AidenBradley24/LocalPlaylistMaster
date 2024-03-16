﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using LocalPlaylistMaster.Backend;
using LocalPlaylistMaster.ValueConverters;

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
            }
        }
        private DatabaseManager? manager;

        public Dictionary<int, string> RemoteReference
        {
            get => ((IntStringMapConverter)Host.Resources["RemoteMap"]).ValueMap;
            set => ((IntStringMapConverter) Host.Resources["RemoteMap"]).ValueMap = value;
        }

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

        #region Edit Records Properties

        CollectionPropertyManager? propertyManager;

        private enum EditType { None, Track, Remote, Playlist }
        private EditType currentEdit;

        private void OnEditTypeChanged()
        {
            OnPropertyChanged(nameof(EditingTrack));
            OnPropertyChanged(nameof(EditingRemote));
            OnPropertyChanged(nameof(EditingPlaylist));
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

        public ICommand NewRemoteCommand { get; }
        public ICommand NewPlaylistCommand { get; }

        public ICommand RemoveTrackSelectionFromDbCommand { get; }
        public ICommand RemoveRemoteSelectionFromDbCommand { get; }
        public ICommand RemovePlaylistSelectionFromDbCommand { get; }

        public ICommand FetchRemoteSelectionCommand { get; }
        public ICommand DownloadRemoteSelectionCommand { get; }
        public ICommand SyncRemoteSelectionCommand { get; }

        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand FetchAllCommand { get; }
        public ICommand DownloadAllCommand { get; }
        public ICommand SyncNewCommand { get; }
        public ICommand DownloadSelectedTracksCommand { get; }

        public MainModel(MainWindow host)
        {
            Host = host;
            tracks = [];
            remotes = [];
            playlists = [];

            NewRemoteCommand = new RelayCommand(AddRemote, HasDb);
            NewPlaylistCommand = new RelayCommand(AddPlaylist, HasDb);

            RemoveTrackSelectionFromDbCommand = new RelayCommand(RemoveTrackSelectionFromDb,
                () => manager != null && EditingTrack);
            RemoveRemoteSelectionFromDbCommand = new RelayCommand(RemoveRemoteSelectionFromDb,
                () => manager != null && EditingRemote);
            RemovePlaylistSelectionFromDbCommand = new RelayCommand(RemoveRemoteSelectionFromDb,
                () => manager != null && EditingPlaylist);

            FetchRemoteSelectionCommand = new RelayCommand(FetchRemoteSelection,
                () => manager != null && EditingRemote);
            DownloadRemoteSelectionCommand = new RelayCommand(DownloadRemoteSelection,
                () => manager != null && EditingRemote);
            SyncRemoteSelectionCommand = new RelayCommand(SyncRemoteSelection,
                () => manager != null && EditingRemote);

            PreviousPageCommand = new RelayCommand(PreviousPage, CanNavigatePrevious);
            NextPageCommand = new RelayCommand(NextPage, CanNavigateNext);
            FetchAllCommand = new RelayCommand(FetchAll, HasDb);
            DownloadAllCommand = new RelayCommand(DownloadAll, HasDb);
            SyncNewCommand = new RelayCommand(SyncNew, HasDb);
            DownloadSelectedTracksCommand = new RelayCommand(DownloadSelectedTracks,
                () => manager != null && EditingTrack);
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

            var task = Manager.GetTracks(VIEW_SIZE, currentTrackOffset);
            task.Wait(); // TODO add loading bar
            Tracks = new ObservableCollection<Track>(task.Result);

            var remoteReferenceTask = Manager.GetRemoteNames(task.Result.Select(t => t.Remote));
            remoteReferenceTask.Wait();
            RemoteReference = remoteReferenceTask.Result;
            RemoteReference.Add(-1, "NONE");

            Host.trackGrid.SelectedItems.Clear();
            OnPropertyChanged(nameof(Tracks));
        }

        public void RefreshRemotes()
        {
            if (Manager == null)
            {
                Remotes = [];
                return;
            }

            var task = Manager.GetRemotes(VIEW_SIZE, currentRemoteOffset);
            task.Wait(); // TODO add loading bar

            Remotes = new ObservableCollection<Remote>(task.Result);
            Host.remoteGrid.SelectedItems.Clear();
            OnPropertyChanged(nameof(Remotes));
        }

        public void RefreshPlaylists()
        {
            if (Manager == null)
            {
                Playlists = [];
                return;
            }

            var task = Manager.GetPlaylists(VIEW_SIZE, currentPlaylistOffset);
            task.Wait(); // TODO add loading bar

            Playlists = new ObservableCollection<Playlist>(task.Result);
            Host.playlistGrid.SelectedItems.Clear();
            OnPropertyChanged(nameof(Playlists));
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
        }

        private bool CanNavigateNext()
        {
            if (Manager == null) return false;

            return Host.CurrentTab.Name switch
            {
                "tracksTab" => currentTrackOffset + VIEW_SIZE < Manager.GetTrackCount(),
                "remotesTab" => currentRemoteOffset + VIEW_SIZE < Manager.GetRemoteCount(),
                "playlistsTab" => currentPlaylistOffset + VIEW_SIZE < Manager.GetPlaylistCount(),
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
                    return;
                }
            }

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

            string[] EDIT_NAMES = [nameof(EditName), nameof(EditDescription)];
            string[] ACTUAL_NAMES = [nameof(Playlist.Name), nameof(Playlist.Description)];

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
                MessageBox.Show("You have unapplied changes.\nCancel them to continue.", "ERROR!", MessageBoxButton.OK, MessageBoxImage.Error);
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
        #endregion
    }
}
