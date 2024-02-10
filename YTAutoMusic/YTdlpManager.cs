using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalPlaylistMaster.Backend
{
    public class YTdlpManager : RemoteManager
    {
        public YTdlpManager(DependencyProcessManager dependencies) : base(dependencies)
        {
        }

        protected override async Task DownloadAudio(string link, IEnumerable<string> remoteIDs, DirectoryInfo downloadDir)
        {
            using Process process = Dependencies.CreateDlpProcess();
            process.StartInfo.Arguments = $"\"{link}\" -P \"{downloadDir.FullName}\" -f bestaudio --force-overwrites --yes-playlist";
            process.Start();
            await process.WaitForExitAsync();
        }

        protected override async Task FetchMetadata(string link, DirectoryInfo downloadDir)
        {
            using Process process = Dependencies.CreateDlpProcess();
            process.StartInfo.Arguments = $"\"{link}\" -P \"{downloadDir.FullName}\" --skip-download --write-info-json --write-playlist-metafiles";
            process.Start();
            await process.WaitForExitAsync();
        }
    }
}
