using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using LocalPlaylistMaster.Backend.Utilities;
using static LocalPlaylistMaster.Backend.Utilities.ProgressModel;
using static LocalPlaylistMaster.Backend.Utilities.YTdlpUtils;

namespace LocalPlaylistMaster.Backend
{
    /// <summary>
    /// YouTube and sites in <seealso href="https://github.com/yt-dlp/yt-dlp/blob/master/supportedsites.md">this list</seealso> download SINGLE VIDEO WITH OR WITHOUT CHAPTERS
    /// </summary>
    /// <param name="remote"></param>
    /// <param name="dependencies"></param>
    public class YTdlpConcertManager(Remote remote, DependencyProcessManager dependencies, DatabaseManager db) : RemoteManager(remote, dependencies), IConcertManager
    {
        public override bool CanFetch => false;
        public override bool CanDownload => false;
        public override bool CanSync => true;

        public IMiscJsonUser JsonUser { get => ExistingRemote; }
        public IConcertManager MeConcert { get => this; }

        public override async Task<(DirectoryInfo downloadDir, Dictionary<string, FileInfo> fileMap)> DownloadAudio(IProgress<(ReportType type, object report)> reporter, IEnumerable<string> remoteIDs)
        {
            throw new NotImplementedException();
        }

        public override async Task<(Remote remote, IEnumerable<Track> tracks)> FetchRemote(IProgress<(ReportType type, object report)> reporter)
        {
            throw new NotImplementedException();
        }

        public override async Task<(Remote remote, IEnumerable<Track> tracks, DirectoryInfo downloadDir, Dictionary<string, FileInfo> fileMap)>
            SyncRemote(IProgress<(ReportType type, object report)> reporter, IEnumerable<string> ignoredIds)
        {
            DirectoryInfo downloadDir = Directory.CreateTempSubdirectory();
            using (Process process = Dependencies.CreateDlpProcess())
            {
                process.StartInfo.Arguments = $"\"{ExistingRemote.Link}\" -P \"{downloadDir.FullName}\" --write-description --no-playlist -f bestaudio";
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.OutputDataReceived += (object sender, DataReceivedEventArgs args) =>
                {
                    if (string.IsNullOrEmpty(args.Data)) return;
                    if (!args.Data.StartsWith("[download]")) return;
                    string percentageString = args.Data.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1].Trim();
                    if (!percentageString.EndsWith('%')) return;
                    int percent = (int)double.Parse(percentageString.TrimEnd('%'));
                    reporter.Report((ReportType.Progress, percent));
                    reporter.Report((ReportType.DetailText, $"Downloading audio: {percentageString}"));
                };
                process.Start();
                process.BeginOutputReadLine();
                await process.WaitForExitAsync();
            }

            reporter.Report((ReportType.DetailText, "reading downloaded files"));
            reporter.Report((ReportType.Progress, -1));

            var files = downloadDir.EnumerateFiles();

            FileInfo descriptionFile = files.Where(f => f.Name.EndsWith(".description")).First();
            string id = GetURLTag(descriptionFile.Name);
            string playlistName = GetNameWithoutURLTag(descriptionFile.Name);
            using var reader = descriptionFile.OpenText();
            string playlistDescription = await reader.ReadToEndAsync();

            FileInfo audioFile = files.Where(f => !f.Name.EndsWith(".description")).First();

            Concert concert = MeConcert.TryGetConcert() ?? new Concert();
            Remote newRemote = new(
                ExistingRemote.Id,
                playlistName,
                playlistDescription,
                ExistingRemote.Link,
                concert.TrackRecords.Count + 1,
                ExistingRemote.Type,
                ExistingRemote.Settings,
                ExistingRemote.MiscJson
                );

            if (concert.TrackRecords.Count > 0)
            {
                reporter.Report((ReportType.Message, new MessageBox()
                {
                    Title = "Unable to sync!",
                    Detail = "Tracks are already added.\nRemove tracks before attempting to sync."
                }));

                return (newRemote, [], downloadDir, []);
            }

            using (Process process = Dependencies.CreateDlpProcess())
            {
                MemoryStream stream = new();
                FileInfo logFile = new(Path.Combine(downloadDir.FullName, "log"));
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                process.StartInfo.Arguments = $"\"{ExistingRemote.Link}\" --dump-json";
                process.OutputDataReceived += (object sender, DataReceivedEventArgs args) =>
                {
                    if (string.IsNullOrEmpty(args.Data)) return;
                    stream.Write(Encoding.UTF8.GetBytes(args.Data));
                };
                process.Start();
                process.BeginOutputReadLine();
                await process.WaitForExitAsync();
                stream.Position = 0;
                using var doc = await JsonDocument.ParseAsync(stream);
                var chapters = doc.RootElement.GetProperty("chapters");
                foreach (var chapter in chapters.EnumerateArray())
                {
                    string title = chapter.GetProperty("title").GetString() ?? "";
                    TimeSpan startTime = TimeSpan.FromSeconds(chapter.GetProperty("start_time").GetDouble());
                    TimeSpan endTime = TimeSpan.FromSeconds(chapter.GetProperty("end_time").GetDouble());
                    concert.TrackRecords.Add(new(title, startTime, endTime, -1));
                }
            }

            ((IMiscJsonUser)newRemote).SetProperty("concert", concert);
            var fileMap = new Dictionary<string, FileInfo>
            {
                { Concert.CONCERT_TRACK, audioFile }
            };

            Track concertTrack = new()
            {
                Name = playlistName,
                Description = playlistDescription,
                RemoteId = Concert.CONCERT_TRACK,
                Remote = ExistingRemote.Id
            };

            return (newRemote, [concertTrack], downloadDir, fileMap);
        }

        public async Task Initialize()
        {
            Concert concert = MeConcert.GetConcert();
            if (concert.ConcertTrackId == -1)
            {
                UserQuery query = new($"remote={ExistingRemote.Id}");
                Track concertTrack = (await db.ExecuteUserQuery(query, 1, 0)).First();
                concert.ConcertTrackId = concertTrack.Id;
                concert.EnsureNamesAreUnique();
                MeConcert.SetConcert(concert);
            }
        }

        /// <summary>
        /// Split concert track into its respective pieces. Orphans the old tracks.
        /// </summary>
        /// <returns></returns>
        public async Task SplitAndCreate(IProgress<(ReportType type, object report)> reporter)
        {
            reporter.Report((ReportType.TitleText, "Splitting concert"));

            Concert concert = MeConcert.GetConcert();
            if (concert.ConcertTrackId == -1) throw new Exception("Not initialized");

            {
                UserQuery query = new($"remote={ExistingRemote.Id}&id!={concert.ConcertTrackId}");
                IEnumerable<int> newOrphans = (await db.ExecuteUserQuery(query, int.MaxValue, 0)).Select(t => t.Id);
                await db.OrphanTracks(newOrphans);
            }

            FileInfo sourceFile = db.GetTrackAudio(concert.ConcertTrackId);
            Dictionary<string, Track> trackMap = [];
            Dictionary<string, FileInfo> fileMap = [];
            DirectoryInfo tempDir = Directory.CreateTempSubdirectory();
            SemaphoreSlim semaphore = new(Math.Max(1, Environment.ProcessorCount / 2));
            int count = concert.TrackRecords.Count;
            int index = -1;
            int completed = 0;
            await Parallel.ForEachAsync(concert.TrackRecords, async (record, _) => 
            {
                int i = Interlocked.Increment(ref index);
                FileInfo tempFile = new(Path.Combine(tempDir.FullName, $"{i}.mp3"));

                var ffmpeg = Dependencies.CreateFfmpegProcess();
                ffmpeg.StartInfo.CreateNoWindow = true;
                string args = $"-y -ss {record.StartTime} -to {record.EndTime} -i \"{sourceFile.FullName}\" -c copy \"{tempFile.FullName}\"";
                ffmpeg.StartInfo.Arguments = args;

                await semaphore.WaitAsync(_);

                try
                {
                    ffmpeg.Start();
                    await ffmpeg.WaitForExitAsync(_);
                }
                finally
                {
                    semaphore.Release();
                }

                Track track = new()
                {
                    Name = record.Name,
                    Description = $"Part of {ExistingRemote.Name}",
                    Remote = ExistingRemote.Id,
                    RemoteId = record.Name,
                    Downloaded = true
                };

                TrackProbe probe = new(tempFile, track, Dependencies);
                await probe.MatchDuration();

                lock (track)
                {
                    trackMap.Add(record.Name, track);
                }

                lock (fileMap)
                {
                    fileMap.Add(record.Name, tempFile);
                }

                lock (reporter)
                {
                    reporter.Report((ReportType.DetailText, $"{++completed}/{count}"));
                    reporter.Report((ReportType.Progress, (int)((float)completed / count * 100)));
                }
            });

            reporter.Report((ReportType.DetailText, "finishing"));

            await db.IngestTracks(trackMap.Values);
            await db.GrabIds(trackMap.Values);

            ExistingRemote.TrackCount = trackMap.Values.Count + 1;
            await db.UpdateRemotes([ExistingRemote]);

            foreach (var record in concert.TrackRecords)
            {
                Track track = trackMap[record.Name];
                record.TrackId = track.Id;
                string source = fileMap[record.Name].FullName;
                string destination = db.GetTrackAudio(track.Id).FullName;
                File.Move(source, destination);
            }

            tempDir.Delete(true);
        }
    }
}