using LocalPlaylistMaster.Backend.Metadata_Fillers;
using System.Diagnostics;
using static LocalPlaylistMaster.Backend.Utilities.ProgressModel;
using static LocalPlaylistMaster.Backend.Utilities.YTdlpUtils;

namespace LocalPlaylistMaster.Backend
{
    /// <summary>
    /// YouTube and sites in <seealso href="https://github.com/yt-dlp/yt-dlp/blob/master/supportedsites.md">this list</seealso> download WHOLE PLAYLIST
    /// </summary>
    /// <param name="remote"></param>
    /// <param name="dependencies"></param>
    internal class YTdlpPlaylistManager(Remote remote, DependencyProcessManager dependencies) : RemoteManager(remote, dependencies)
    {
        public override bool CanFetch => true;
        public override bool CanDownload => true;
        public override bool CanSync => true;

        public override async Task<(DirectoryInfo downloadDir, Dictionary<string, FileInfo> fileMap)> DownloadAudio(IProgress<(ReportType type, object report)> reporter, IEnumerable<string> remoteIDs)
        {
            DirectoryInfo downloadDir = Directory.CreateTempSubdirectory();
            using Process process = Dependencies.CreateDlpProcess();
            string idFilter = string.Join(' ', remoteIDs.Select(s => $"--match-filter id={s}"));
            process.StartInfo.Arguments = $"\"{ExistingRemote.Link}\" -P \"{downloadDir.FullName}\" -f bestaudio --yes-playlist {idFilter}";
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += (object sender, DataReceivedEventArgs args) =>
            {
                if (string.IsNullOrEmpty(args.Data)) return;
                if (args.Data.StartsWith("[download] Downloading item "))
                {
                    reporter.Report((ReportType.DetailText, $"downloading audio\n{args.Data}"));
                    string progText = args.Data["[download] Downloading item ".Length..];
                    string[] vals = progText.Split("of");
                    int progressValue = (int)((float)int.Parse(vals[0].Trim()) / int.Parse(vals[1].Trim()) * 100);
                    reporter.Report((ReportType.Progress, progressValue));
                }
            };
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.BeginOutputReadLine();
            await process.WaitForExitAsync();

            reporter.Report((ReportType.DetailText, "reading downloaded files"));

            Dictionary<string, FileInfo> fileMap = [];
            foreach(var file in downloadDir.EnumerateFiles())
            {
                fileMap.Add(GetURLTag(file.Name), file);
            }

            return (downloadDir, fileMap);
        }

        public override async Task<(Remote remote, IEnumerable<Track> tracks)> FetchRemote(IProgress<(ReportType type, object report)> reporter)
        {
            DirectoryInfo downloadDir = Directory.CreateTempSubdirectory();
            using Process process = Dependencies.CreateDlpProcess();
            process.StartInfo.Arguments = $"\"{ExistingRemote.Link}\" -P \"{downloadDir.FullName}\" --skip-download --write-description --write-playlist-metafiles --yes-playlist";
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += (object sender, DataReceivedEventArgs args) => 
            {
                if (string.IsNullOrEmpty(args.Data)) return;
                if (args.Data.StartsWith("[download] Downloading item "))
                {
                    reporter.Report((ReportType.DetailText, $"fetching remote\n{args.Data}"));
                    string progText = args.Data["[download] Downloading item ".Length..];
                    string[] vals = progText.Split("of");
                    int progressValue = (int)((float)int.Parse(vals[0].Trim()) / int.Parse(vals[1].Trim()) * 100);
                    reporter.Report((ReportType.Progress, progressValue));
                }
            };
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.BeginOutputReadLine();
            await process.WaitForExitAsync();

            string playlistId = GetPlaylistId(ExistingRemote.Link);

            List<Track> tracks = [];
            string playlistName = "playlist";
            string playlistDescription = "";

            reporter.Report((ReportType.DetailText, "reading downloaded files"));

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

                Track track = new()
                {
                    Name = name,
                    Remote = ExistingRemote.Id,
                    RemoteId = id,
                    Description = description,
                };

                tracks.Add(track);
                counter++;
            }

            downloadDir.Delete(true);
            tracks = MetadataFiller.ApplyMetadataFillerSuite(typeof(YTSuite), tracks);

            return (new Remote(ExistingRemote.Id, playlistName, playlistDescription, ExistingRemote.Link, counter,
                ExistingRemote.Type, ExistingRemote.Settings, "{}"), tracks);
        }

        public override async Task<(Remote remote, IEnumerable<Track> tracks, DirectoryInfo downloadDir, Dictionary<string, FileInfo> fileMap)> 
            FetchAndDownload(IProgress<(ReportType type, object report)> reporter, IEnumerable<string> ignoredIds)
        {
            DirectoryInfo downloadDir = Directory.CreateTempSubdirectory();
            using Process process = Dependencies.CreateDlpProcess();
            string idFilter = ignoredIds.Any() ? "--match-filter " + string.Join('&', ignoredIds.Select(s => $"id!={s}")) : "";
            process.StartInfo.Arguments = $"\"{ExistingRemote.Link}\" -P \"{downloadDir.FullName}\" -f bestaudio --write-description --write-playlist-metafiles --yes-playlist {idFilter}";
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += (object sender, DataReceivedEventArgs args) =>
            {
                if (string.IsNullOrEmpty(args.Data)) return;
                if (args.Data.StartsWith("[download] Downloading item "))
                {
                    reporter.Report((ReportType.DetailText, $"downloading remote\n{args.Data}"));
                    string progText = args.Data["[download] Downloading item ".Length..];
                    string[] vals = progText.Split("of");
                    int progressValue = (int)((float)int.Parse(vals[0].Trim()) / int.Parse(vals[1].Trim()) * 100);
                    reporter.Report((ReportType.Progress, progressValue));
                }
            };
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.BeginOutputReadLine();
            await process.WaitForExitAsync();

            string playlistId = GetPlaylistId(ExistingRemote.Link);

            List<Track> tracks = [];
            string playlistName = "playlist";
            string playlistDescription = "";

            reporter.Report((ReportType.DetailText, "reading downloaded files"));

            Dictionary<string, FileInfo> fileMap = [];

            int counter = 0;
            foreach (FileInfo file in downloadDir.EnumerateFiles())
            {
                string id = GetURLTag(file.Name);

                if (file.Name.EndsWith(".description"))
                {
                    string name = GetNameWithoutURLTag(file.Name);
                    using (var reader = file.OpenText())
                    {
                        string description = await reader.ReadToEndAsync();

                        if (id == playlistId)
                        {
                            playlistName = name;
                            playlistDescription = description;
                            continue;
                        }

                        Track track = new()
                        {
                            Name = name,
                            Remote = ExistingRemote.Id,
                            RemoteId = id,
                            Description = description,
                        };

                        tracks.Add(track);
                        counter++;
                    }

                    file.Delete();
                }
                else
                {
                    // audio file
                    fileMap.Add(id, file);
                }
            }

            tracks = MetadataFiller.ApplyMetadataFillerSuite(typeof(YTSuite), tracks);

            return (new Remote(ExistingRemote.Id, playlistName, playlistDescription, ExistingRemote.Link, counter,
                ExistingRemote.Type, ExistingRemote.Settings, "{}"), tracks, downloadDir, fileMap);
        }
    }
}
