namespace LocalPlaylistMaster.Backend
{
    /// <summary>
    /// Prepares to export playlists to multiple formats
    /// </summary>
    public sealed class PlaylistExportManager
    {
        internal const int MAX_PLAYLIST_SIZE = 500;

        public IEnumerable<Track> ValidTracks { get; }
        public IEnumerable<Track> InvalidTracks { get; }
        public Playlist Playlist { get; }

        public enum ExportType { folder, xspf }
        public ExportType Type { get; set; }

        private DirectoryInfo trackDir;

        internal PlaylistExportManager(Playlist playlist, IEnumerable<Track> allTracks, DirectoryInfo trackDir)
        {
            Playlist = playlist;
            allTracks = allTracks.Where(t => !t.Settings.HasFlag(TrackSettings.removeMe));
            ValidTracks = allTracks.Where(t => t.Settings.HasFlag(TrackSettings.downloaded));
            InvalidTracks = allTracks.Where(t => !t.Settings.HasFlag(TrackSettings.downloaded));
            this.trackDir = trackDir;
        }

        public async Task Export()
        {

        }
    }
}
