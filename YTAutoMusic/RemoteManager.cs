using static LocalPlaylistMaster.Backend.ProgressModel;

namespace LocalPlaylistMaster.Backend
{
    /// <summary>
    /// Manages download and syncing from remotes
    /// </summary>
    public abstract class RemoteManager
    {
        internal Remote ExistingRemote { get; init; }
        protected DependencyProcessManager Dependencies { get; init; }

        public RemoteManager(Remote remote, DependencyProcessManager dependencies)
        {
            Dependencies = dependencies;
            ExistingRemote = remote;
        }

        /// <summary>
        /// Fetches tracks from a remote server with unaltered metadata.
        /// </summary>
        /// <returns>A remote record and a collection of track records</returns>
        public abstract Task<(Remote remote, IEnumerable<Track> tracks)> FetchRemote(IProgress<(ReportType type, object report)> reporter);

        /// <summary>
        /// Downloads raw audio from remote.
        /// </summary>
        /// <param name="remoteIDs">Collection of track remoteIDs</param>
        /// <returns>Directory with raw files marked with their remote ids and a file extension</returns>
        public abstract Task<DirectoryInfo> DownloadAudio(IEnumerable<string> remoteIDs);
    }  
}
