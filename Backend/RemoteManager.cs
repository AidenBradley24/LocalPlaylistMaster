using static LocalPlaylistMaster.Backend.Utilities.ProgressModel;

namespace LocalPlaylistMaster.Backend
{
    public enum RemoteType
    {
        UNINITIALIZED = -1, ytdlp_playlist, ytdlp_concert, local_folder,
    }

    /// <summary>
    /// Manages download and syncing from remotes
    /// </summary>
    public abstract class RemoteManager(Remote remote, DependencyProcessManager dependencies)
    {
        internal Remote ExistingRemote { get; init; } = remote;
        protected DependencyProcessManager Dependencies { get; init; } = dependencies;

        public static RemoteManager Create(Remote remote, DependencyProcessManager dependencies, DatabaseManager db)
        {
            return remote.Type switch
            {
                RemoteType.ytdlp_playlist => new YTdlpPlaylistManager(remote, dependencies),
                RemoteType.ytdlp_concert => new YTdlpConcertManager(remote, dependencies, db),
                RemoteType.local_folder => new LocalPlaylistManager(remote, dependencies),
                _ => throw new Exception("Invalid remote type")
            };
        }

        public abstract bool CanFetch { get; }
        public abstract bool CanDownload { get; }
        public abstract bool CanSync { get; }

        /// <summary>
        /// Fetches track metadata from a remote server.
        /// </summary>
        /// <returns>A remote record and a collection of track records</returns>
        public abstract Task<(Remote remote, IEnumerable<Track> tracks)> FetchRemote(IProgress<(ReportType type, object report)> reporter);

        /// <summary>
        /// Downloads raw audio from a remote server.
        /// </summary>
        /// <param name="remoteIDs">Tracks which to fetch</param>
        /// <returns>Directory with raw files marked with their remote ids and a file extension</returns>
        public abstract Task<(DirectoryInfo? downloadDir, Dictionary<string, FileInfo> fileMap)> 
            DownloadAudio(IProgress<(ReportType type, object report)> reporter, IEnumerable<string> remoteIDs);

        /// <summary>
        /// Fetches and downloads tracks at the same time from a remote server.
        /// </summary>
        /// <param name="ignoredIds">Tracks which to ignore on download</param>
        /// <returns></returns>
        public abstract Task<(Remote remote, IEnumerable<Track> tracks, DirectoryInfo? downloadDir, Dictionary<string, FileInfo> fileMap)> 
            SyncRemote(IProgress<(ReportType type, object report)> reporter, IEnumerable<string> ignoredIds);
    }
}
