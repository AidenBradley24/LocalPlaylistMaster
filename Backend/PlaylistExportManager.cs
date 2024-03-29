using System.Text.RegularExpressions;
using static LocalPlaylistMaster.Backend.ProgressModel;

namespace LocalPlaylistMaster.Backend
{
    /// <summary>
    /// Prepares to export playlists to multiple formats
    /// </summary>
    public sealed partial class PlaylistExportManager
    {
        internal const int MAX_PLAYLIST_SIZE = 500;

        public IEnumerable<Track>? ValidTracks { get; private set; }
        public IEnumerable<Track>? InvalidTracks { get; private set; }
        public Playlist? Playlist { get; set; }
        public DirectoryInfo? OutputDir { get; set; }

        public enum ExportType { folder, xspf }
        public ExportType Type { get; set; }

        private DirectoryInfo? inputDir;
        private ExportType type;
        private int length = 0;

        public void Setup(DatabaseManager db)
        {
            if(Playlist == null) throw new Exception("No playlist found");
            UserQuery query = new(Playlist.Tracks);
            var task = db.ExecuteUserQuery(query, MAX_PLAYLIST_SIZE, 0);
            task.Wait();
            IEnumerable<Track> allTracks = task.Result;
            allTracks = allTracks.Where(t => !t.Settings.HasFlag(TrackSettings.removeMe));
            ValidTracks = allTracks.Where(t => t.Settings.HasFlag(TrackSettings.downloaded));
            InvalidTracks = allTracks.Where(t => !t.Settings.HasFlag(TrackSettings.downloaded));
            length = ValidTracks.Count();
        }

        public async Task Export(IProgress<(ReportType type, object report)> reporter)
        {
            if (Playlist == null) throw new Exception("No playlist found");
            if (OutputDir == null) throw new Exception("Setup first");
            if (ValidTracks == null) throw new Exception("Setup first");
            if (length == 0) throw new Exception("No tracks in playlist");

            reporter.Report((ReportType.TitleText, "Generating playlist"));

            if(Type == ExportType.folder)
            {
                await FillFinalTrackDir(OutputDir, reporter);
                return;
            }

            switch(Type)
            {
                case ExportType.xspf:
                    break;
            }

            var trackDir = Directory.CreateDirectory(Path.Combine(OutputDir.FullName, "tracks"));
            await FillFinalTrackDir(trackDir, reporter);
        }

        private async Task FillFinalTrackDir(DirectoryInfo trackDir, IProgress<(ReportType type, object report)> reporter)
        {
            if (ValidTracks == null) throw new Exception("Setup first");
            reporter.Report((ReportType.DetailText, "formatting files"));
            int i = 0;
            await Task.Run(() => Parallel.ForEach(ValidTracks, (track, _) =>
            {
                FileInfo oldFile = new(Path.Join(trackDir.FullName,
                $"{track.Id}.{ConversionHandeler.TARGET_FILE_EXTENSION}")); // TODO make this a seperate function in db
                FileInfo newFile = new(Path.Join(
                    trackDir.FullName,
                    $"{CleanName().Replace(track.Name, "").Trim()}.{ConversionHandeler.TARGET_FILE_EXTENSION}"));
                File.Copy(oldFile.FullName, newFile.FullName, true);

                using (var tagFile = TagLib.File.Create(newFile.FullName))
                {
                    tagFile.Tag.Title = track.Name;
                    tagFile.Tag.Description = track.Description;
                    tagFile.Tag.Album = track.Album;
                    tagFile.Tag.Length = track.LengthString;
                    tagFile.Tag.Performers = track.GetArtists();
                    tagFile.Save();
                }

                lock (reporter)
                {
                    reporter.Report((ReportType.Progress, ++i / (float)length));
                }
            }));
        }

        [GeneratedRegex("[\\/:*?\"<>|]")]
        private static partial Regex CleanName();
    }
}
