using System.Diagnostics;

namespace LocalPlaylistMaster.Backend
{
    public class YTdlpManager : RemoteManager
    {
        public YTdlpManager(Remote remote, DependencyProcessManager dependencies) : base(remote, dependencies) {}

        public override async Task<DirectoryInfo> DownloadAudio(IEnumerable<string> remoteIDs)
        {
            // TODO only download given remote ids
            // TODO rename downloaded files (or map them)

            DirectoryInfo downloadDir = Directory.CreateTempSubdirectory();
            using Process process = Dependencies.CreateDlpProcess();
            process.StartInfo.Arguments = $"\"{ExistingRemote.Link}\" -P \"{downloadDir.FullName}\" -f bestaudio --force-overwrites --yes-playlist";
            process.Start();
            await process.WaitForExitAsync();

            return downloadDir;
        }

        public override async Task<(Remote remote, IEnumerable<Track> tracks)> FetchRemote()
        {
            DirectoryInfo downloadDir = Directory.CreateTempSubdirectory();
            using Process process = Dependencies.CreateDlpProcess();
            process.StartInfo.Arguments = $"\"{ExistingRemote.Link}\" -P \"{downloadDir.FullName}\" --skip-download --write-description --write-playlist-metafiles";
            process.Start();
            process.WaitForExit();

            string playlistId = GetPlaylistId(ExistingRemote.Link);

            List<Track> tracks = new();
            string playlistName = "playlist";
            string playlistDescription = "";

            int counter = 0;
            foreach (FileInfo file in downloadDir.EnumerateFiles())
            {
                string id = GetURLTag(file.Name);
                string name = GetNameWithoutURLTag(file.Name);
                using var reader = file.OpenText();
                string description = await reader.ReadToEndAsync();

                if (id == playlistId)
                {
                    playlistName = name;
                    playlistDescription = description;
                    continue;
                }

                Track track = new(Track.UNINITIALIZED, name, ExistingRemote.Id, id, "", "",
                    description, Track.UNINITIALIZED, Track.UNINITIALIZED, TrackSettings.none);
                tracks.Add(track);
                counter++;
            }

            downloadDir.Delete(true);
            return (new Remote(ExistingRemote.Id, playlistName, playlistDescription, ExistingRemote.Link, counter, ExistingRemote.Type, ExistingRemote.Settings), tracks);
        }

        public static string GetNameWithoutURLTag(string name)
        {
            return name[..name.LastIndexOf('[')].Trim();
        }

        public static string GetURLTag(string name)
        {
            return name[(name.LastIndexOf('[') + 1)..name.LastIndexOf(']')];
        }

        public static string GetPlaylistId(string url)
        {
            string playlistID;
            const string LIST_URL = "list=";
            if (url.Contains('&'))
            {
                playlistID = url.Split('&').Where(s => s.StartsWith(LIST_URL)).First()[LIST_URL.Length..];
            }
            else
            {
                int listIndex = url.IndexOf(LIST_URL) + LIST_URL.Length;
                playlistID = url[listIndex..];
            }
            return playlistID;
        }
    }
}
