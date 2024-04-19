using LocalPlaylistMaster.Backend.Metadata_Fillers;
using System.Diagnostics;
using static LocalPlaylistMaster.Backend.ProgressModel;

namespace LocalPlaylistMaster.Backend
{
    public class YTdlpManager(Remote remote, DependencyProcessManager dependencies) : RemoteManager(remote, dependencies)
    {
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
