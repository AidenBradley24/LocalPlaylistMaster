using static LocalPlaylistMaster.Backend.Utilities.ProgressModel;

namespace LocalPlaylistMaster.Backend
{
    /// <summary>
    /// Import a directory of music
    /// </summary>
    /// <param name="remote"></param>
    /// <param name="dependencies"></param>
    public class LocalPlaylistManager(Remote remote, DependencyProcessManager dependencies) : RemoteManager(remote, dependencies)
    {
        public override bool CanFetch => true;
        public override bool CanDownload => true;
        public override bool CanSync => true;

        private readonly HashSet<string> AUDIO_FORMATS = [".mp3", ".aac", ".wma", ".ogg", ".flac", ".wav", ".aiff", ".alac", ".m4a"];

        public override async Task<(DirectoryInfo? downloadDir, Dictionary<string, FileInfo> fileMap)> DownloadAudio(IProgress<(ReportType type, object report)> reporter, IEnumerable<string> remoteIDs)
        {
            var fileMap = GetFileMap(true, remoteIDs);
            return (null, fileMap);
        }

        public override async Task<(Remote remote, IEnumerable<Track> tracks)> FetchRemote(IProgress<(ReportType type, object report)> reporter)
        {
            Remote remote = new(ExistingRemote.Id, "", "", ExistingRemote.Link, 0, ExistingRemote.Type, ExistingRemote.Settings, "{}");
            (remote, List<Track> tracks) = ReadMetadata(remote);
            return (remote, tracks);
        }

        public override async Task<(Remote remote, IEnumerable<Track> tracks, DirectoryInfo? downloadDir, Dictionary<string, FileInfo> fileMap)> SyncRemote(IProgress<(ReportType type, object report)> reporter, IEnumerable<string> ignoredIds)
        {
            var fileMap = GetFileMap(false, ignoredIds);
            Remote remote = new(ExistingRemote.Id, "", "", ExistingRemote.Link, 0, ExistingRemote.Type, ExistingRemote.Settings, "{}");
            (remote, List<Track> tracks) = ReadMetadata(remote, ignoredIds);
            return (remote, tracks, null, fileMap);
        }

        private FileInfo[] GetAudioFiles()
        {
            DirectoryInfo sourceDir = new(ExistingRemote.Link);
            if (!sourceDir.Exists)
            {
                throw new FileNotFoundException("The directory was not found: " + sourceDir.FullName);
            }
            return sourceDir.EnumerateFiles().Where(f => AUDIO_FORMATS.Contains(f.Extension)).ToArray();
        }

        private Dictionary<string, FileInfo> GetFileMap(bool include, IEnumerable<string> inclusion)
        {
            HashSet<string> set = [.. inclusion];
            Dictionary<string, FileInfo> fileMap = [];
            foreach (var file in GetAudioFiles())
            {
                if (include ^ set.Contains(file.Name)) continue;
                fileMap.Add(file.Name, file);
            }
            return fileMap;
        }

        private (Remote remote, List<Track>) ReadMetadata(Remote remote, IEnumerable<string>? exclusion = null)
        {
            List<Track> tracks = [];
            FileInfo[] files = GetAudioFiles();
            HashSet<string> set = [.. exclusion];
            foreach (FileInfo file in files)
            {
                if (set.Contains(file.Name)) continue;
                TagLib.File tagfile = TagLib.File.Create(file.FullName);

                Track track;
                if (string.IsNullOrWhiteSpace(tagfile.Tag.Title))
                {
                    string name = file.Name;
                    int i = name.LastIndexOf('.');
                    name = name[..i];
                    track = new()
                    {
                        Name = name,
                    };
                }
                else
                {
                    track = new()
                    {
                        Name = tagfile.Tag.Title ?? "",
                        Description = tagfile.Tag.Description ?? "",
                        Album = tagfile.Tag.Album ?? "",
                        Artists = string.Join(',', tagfile.Tag.Performers ?? [])
                    };
                }

                track.RemoteId = file.Name;
                track.Remote = ExistingRemote.Id;
                tracks.Add(track);
            }

            DirectoryInfo sourceDir = new(ExistingRemote.Link);
            remote.Name = sourceDir.Name;
            remote.TrackCount = tracks.Count;
            return (remote, tracks);
        }
    }
}
