using System.Diagnostics;
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
                process.Start();
                await process.WaitForExitAsync();
            }

            reporter.Report((ReportType.DetailText, "reading downloaded files"));

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

            MeConcert.SetConcert(concert);
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
                MeConcert.SetConcert(concert);
            }
        }

        /// <summary>
        /// Split concert track into its respective pieces. Orphans the old tracks.
        /// </summary>
        /// <returns></returns>
        public async Task SplitAndCreate()
        {
            Concert concert = MeConcert.GetConcert();
            if (concert.ConcertTrackId == -1) throw new Exception("Not initialized");

            {
                UserQuery query = new($"remote={ExistingRemote.Id}&id!={concert.ConcertTrackId}");
                IEnumerable<int> newOrphans = (await db.ExecuteUserQuery(query, int.MaxValue, 0)).Select(t => t.Id);
                await db.OrphanTracks(newOrphans);
            }

            FileInfo sourceFile = db.GetTrackAudio(concert.ConcertTrackId);
            Dictionary<Concert.TrackRecord, Track> trackMap = [];
            Dictionary<Concert.TrackRecord, FileInfo> fileMap = [];
            DirectoryInfo tempDir = Directory.CreateTempSubdirectory();
            SemaphoreSlim semaphore = new(Math.Max(1, Environment.ProcessorCount / 2));

            int index = -1;
            await Parallel.ForEachAsync(concert.TrackRecords, async (record, _) => 
            {
                int i = Interlocked.Increment(ref index);
                FileInfo tempFile = new(Path.Combine(tempDir.FullName, $"{i}.mp3"));

                var ffmpeg = Dependencies.CreateFfmpegProcess();
                //ffmpeg.StartInfo.CreateNoWindow = true;
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
                    trackMap.Add(record, track);
                }

                lock (fileMap)
                {
                    fileMap.Add(record, tempFile);
                }
            });

            await db.IngestTracks(trackMap.Values);
            await db.GrabIds(trackMap.Values);
            
            foreach(var record in concert.TrackRecords)
            {
                Track track = trackMap[record];
                record.TrackId = track.Id;
                File.Move(fileMap[record].FullName, db.GetTrackAudio(track.Id).FullName);
            }

            tempDir.Delete(true);
        }
    }
}