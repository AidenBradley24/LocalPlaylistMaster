using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalPlaylistMaster.Backend
{
    /// <summary>
    /// Manages download and syncing from remotes
    /// </summary>
    public abstract class RemoteManager
    {
        protected DependencyProcessManager Dependencies { get; init; }
        public RemoteSettings Settings { get; init; }

        public RemoteManager(DependencyProcessManager dependencies, RemoteSettings settings)
        {
            Dependencies = dependencies;
            Settings = settings;
        }

        /// <summary>
        /// Fetches tracks from a remote server with unaltered metadata.
        /// </summary>
        /// <returns>Collection of track records</returns>
        public abstract Task<(string playlistName, string playlistDescription, IEnumerable<Track> tracks)> FetchRemote();

        /// <summary>
        /// Downloads raw audio from remote.
        /// </summary>
        /// <param name="remoteIDs">Collection of track remoteIDs</param>
        /// <returns>Directory with raw files marked with their remote ids and a file extension</returns>
        public abstract Task<DirectoryInfo> DownloadAudio(IEnumerable<string> remoteIDs);
    }

    public enum RemoteType { ytdlp }
    public enum RemoteSettings { removeMe, locked }
}
