using System;
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
        protected DependencyProcessManager Dependencies { get; private set; }

        public RemoteManager(DependencyProcessManager dependencies)
        {
            Dependencies = dependencies;
        }

        public async Task<DirectoryInfo> ReadyMetadata(string link)
        {
            var dir = Directory.CreateTempSubdirectory();
            await FetchMetadata(link, dir);
            return dir;
        }

        protected abstract Task FetchMetadata(string link, DirectoryInfo downloadDir);
        protected abstract Task DownloadAudio(string link, IEnumerable<string> remoteIDs, DirectoryInfo downloadDir);
    }
}
