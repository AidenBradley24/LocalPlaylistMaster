using System.ComponentModel;
using static LocalPlaylistMaster.Backend.ProgressModel;
using static LocalPlaylistMaster.Backend.Extensions;
using LocalPlaylistMaster.Backend.Playlist_Files;

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

        public ExportType Type { get; set; }

        public bool HasLib { get => Type == ExportType.xspflib || Type == ExportType.m3u8lib; }

        private DirectoryInfo? inputDir;
        private int length = 0;

        public async Task Setup(DatabaseManager db)
        {
            if(Playlist == null) throw new Exception("No playlist found");
            UserQuery query = new(Playlist.Tracks);
            IEnumerable<Track> allTracks = await db.ExecuteUserQuery(query, MAX_PLAYLIST_SIZE, 0);
            allTracks = allTracks.Where(t => !t.Settings.HasFlag(TrackSettings.removeMe));
            ValidTracks = allTracks.Where(t => t.Settings.HasFlag(TrackSettings.downloaded));
            InvalidTracks = allTracks.Where(t => !t.Settings.HasFlag(TrackSettings.downloaded));
            length = ValidTracks.Count();
            inputDir = db.GetAudioDir();
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

            Dictionary<FileInfo, Track> map;
            if (HasLib)
            {
                map = MapFilesFromLibrary();
            }
            else
            {
                var trackDir = Directory.CreateDirectory(Path.Combine(OutputDir.FullName, "tracks"));
                map = await FillFinalTrackDir(trackDir, reporter);
            }   

            PlaylistFile playlistFile = Type switch
            {
                ExportType.xspf or ExportType.xspflib => new XspfFile(),
                ExportType.m3u8 or ExportType.m3u8lib => new M3U8File(),
                _ => throw new Exception("not a valid playlist file")
            };
            playlistFile.Build(OutputDir, Playlist, map, HasLib);
        }

        private async Task<Dictionary<FileInfo, Track>> FillFinalTrackDir(DirectoryInfo outputDir, IProgress<(ReportType type, object report)> reporter)
        {
            if (ValidTracks == null) throw new Exception("Setup first");
            if (inputDir == null) throw new Exception("Setup first");
            if(!outputDir.Exists) outputDir.Create();

            Dictionary<FileInfo, Track> trackFileMap = [];

            reporter.Report((ReportType.DetailText, "formatting files"));
            int i = 0;
            await Task.Run(() => Parallel.ForEach(ValidTracks, (track, _) =>
            {
                FileInfo oldFile = new(Path.Join(inputDir.FullName,
                $"{track.Id}.{ConversionHandeler.TARGET_FILE_EXTENSION}"));
                FileInfo newFile = new(Path.Join(
                    outputDir.FullName,
                    $"{CleanName(track.Name)}.{ConversionHandeler.TARGET_FILE_EXTENSION}"));

                if (!newFile.Exists)
                {
                    File.Copy(oldFile.FullName, newFile.FullName, true);
                    using var tagFile = TagLib.File.Create(newFile.FullName);
                    tagFile.Tag.Title = track.Name;
                    tagFile.Tag.Description = track.Description;
                    tagFile.Tag.Album = track.Album;
                    tagFile.Tag.Length = track.LengthString;
                    tagFile.Tag.Performers = track.GetArtists();
                    tagFile.Save();
                }

                lock (trackFileMap)
                {
                    trackFileMap.Add(newFile, track);
                }

                lock (reporter)
                {
                    int progress = (int)(100 * (++i / (float)length));
                    reporter.Report((ReportType.Progress, progress));
                }
            }));

            return trackFileMap;
        }

        private Dictionary<FileInfo, Track> MapFilesFromLibrary()
        {
            if (ValidTracks == null) throw new Exception("Setup first");
            if (inputDir == null) throw new Exception("Setup first");

            Dictionary<FileInfo, Track> trackFileMap = [];
            foreach (Track track in ValidTracks)
            {
                FileInfo audioFile = new(Path.Join(inputDir.FullName, $"{track.Id}.{ConversionHandeler.TARGET_FILE_EXTENSION}"));
                trackFileMap.Add(audioFile, track);
            }
            return trackFileMap;
        }
    }

    public enum ExportType 
    {
        [Description("Export mp3s to folder")]
        folder,
        [Description("Export to a xspf playlist, copying mp3s")]
        xspf,
        [Description("Export to a m3u8 playlist, copying mp3s")]
        m3u8,
        [Description("Export to a xspf playlist, using library mp3s")]
        xspflib,
        [Description("Export to a m3u8 playlist, using library mp3s")]
        m3u8lib,
    }
}
