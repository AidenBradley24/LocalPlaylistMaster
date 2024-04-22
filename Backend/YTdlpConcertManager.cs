using LocalPlaylistMaster.Backend.Utilities;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using static LocalPlaylistMaster.Backend.ProgressModel;
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

        public override async Task<(DirectoryInfo downloadDir, Dictionary<string, FileInfo> fileMap)> DownloadAudio(IProgress<(ReportType type, object report)> reporter, IEnumerable<string> remoteIDs)
        {
            throw new NotImplementedException();
        }

        public override async Task<(Remote remote, IEnumerable<Track> tracks)> FetchRemote(IProgress<(ReportType type, object report)> reporter)
        {
            throw new NotImplementedException();
        }

        public override async Task<(Remote remote, IEnumerable<Track> tracks, DirectoryInfo downloadDir, Dictionary<string, FileInfo> fileMap)>
            FetchAndDownload(IProgress<(ReportType type, object report)> reporter, IEnumerable<string> ignoredIds)
        {
            DirectoryInfo downloadDir = Directory.CreateTempSubdirectory();
            using (Process process = Dependencies.CreateDlpProcess())
            {
                process.StartInfo.Arguments = $"\"{ExistingRemote.Link}\" -P \"{downloadDir.FullName}\" --write-description --no-playlist";
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

            Concert concert = ((IMiscJsonUser)ExistingRemote).GetProperty<Concert>("concert") ?? new Concert();
            Remote newRemote = new(
                ExistingRemote.Id,
                playlistName,
                playlistDescription,
                ExistingRemote.Link,
                concert.trackRecords.Count + 1,
                ExistingRemote.Type,
                ExistingRemote.Settings,
                ExistingRemote.MiscJson
                );

            if (concert.trackRecords.Count > 0)
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
                process.StartInfo.Arguments = $"yt-dlp --dump-json \"{ExistingRemote.Link}\"";
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                process.Start();
                await process.WaitForExitAsync();
                using var doc = await JsonDocument.ParseAsync(process.StandardOutput.BaseStream);
                var chapters = doc.RootElement.GetProperty("chapters");
                foreach (var chapter in chapters.EnumerateArray())
                {
                    string title = chapter.GetProperty("title").GetString() ?? "";
                    TimeSpan startTime = TimeSpan.FromSeconds(chapter.GetProperty("start_time").GetDouble());
                    TimeSpan endTime = TimeSpan.FromSeconds(chapter.GetProperty("end_time").GetDouble());
                    concert.trackRecords.Add(new(title, startTime, endTime, -1));
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
                RemoteId = Concert.CONCERT_TRACK
            };

            return (newRemote, [concertTrack], downloadDir, fileMap);
        }

        public async Task Initialize()
        {
            Concert concert = ((IMiscJsonUser)ExistingRemote).GetProperty<Concert>("concert") ?? throw new Exception("Missing concert element");
            if (concert.concertTrackId == -1)
            {
                UserQuery query = new($"remote={ExistingRemote.Id}");
                Track concertTrack = (await db.ExecuteUserQuery(query, 1, 0)).First();
                concert.concertTrackId = concertTrack.Id;
                ((IMiscJsonUser)ExistingRemote).SetProperty("concert", concert);
            }
        }

        /// <summary>
        /// Split concert track into its respective pieces. Orphans the old tracks.
        /// </summary>
        /// <returns></returns>
        public async Task SplitAndCreate()
        {
            Concert concert = ((IMiscJsonUser)ExistingRemote).GetProperty<Concert>("concert") ?? throw new Exception("Missing concert element");
            if (concert.concertTrackId == -1) throw new Exception("Not initialized");

            {
                UserQuery query = new($"remote={ExistingRemote.Id}&id!={concert.concertTrackId}");
                IEnumerable<int> newOrphans = (await db.ExecuteUserQuery(query, int.MaxValue, 0)).Select(t => t.Id);
                await db.OrphanTracks(newOrphans);
            }

            FileInfo sourceFile = db.GetTrackAudio(concert.concertTrackId);
            Dictionary<Concert.TrackRecord, Track> trackMap = [];
            Dictionary<Concert.TrackRecord, FileInfo> fileMap = [];
            DirectoryInfo tempDir = Directory.CreateTempSubdirectory();
            Semaphore semaphore = new(0, Math.Max(1, Environment.ProcessorCount / 2));
            Mutex indexMutex = new();
            int index = 0;
            await Parallel.ForEachAsync(concert.trackRecords, async (record, _) => 
            {
                indexMutex.WaitOne();
                int i = index++;
                indexMutex.ReleaseMutex();
                FileInfo tempFile = new(Path.Combine(tempDir.FullName, $"{i}.mp3"));

                var ffmpeg = Dependencies.CreateFfmpegProcess();
                ffmpeg.StartInfo.CreateNoWindow = true;
                string args = $"-y -ss {record.StartTime} -to {record.EndTime} -i \"{sourceFile.FullName}\" -c copy \"{tempFile.FullName}\"";
                ffmpeg.StartInfo.Arguments = args;

                semaphore.WaitOne();
                ffmpeg.Start();
                await ffmpeg.WaitForExitAsync(_);
                semaphore.Release();

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
            
            foreach(var record in concert.trackRecords)
            {
                Track track = trackMap[record];
                record.TrackId = track.Id;
                File.Move(fileMap[record].FullName, db.GetTrackAudio(track.Id).FullName);
            }

            tempDir.Delete(true);
        }
    }
}