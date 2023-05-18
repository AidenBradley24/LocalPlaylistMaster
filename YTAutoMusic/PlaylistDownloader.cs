using System.Diagnostics;
using System.Text;
using TagLib.Id3v2;

namespace YTAutoMusic
{
    internal static class PlaylistDownloader
    {
        static readonly string LIST_URL = "list=";

        public static void Create(string dlpPath, string ffmpegPath)
        {
            string folder;

            DirectoryInfo baseDirectory;

            while (true)
            {
                Console.WriteLine("Provide root directory.\nInsert nothing to put in music directory.");
                folder = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(folder))
                {
                    folder = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                    Console.WriteLine(folder);
                }

                try
                {
                    folder = Path.GetFullPath(folder);
                }
                catch
                {
                    Console.WriteLine($"{folder} is not a valid directory.");
                    continue;
                }

                baseDirectory = Directory.CreateDirectory(folder);

                if (IsInsideProject(baseDirectory))
                {
                    Console.WriteLine("Cannot open here.");
                    continue;
                }

                break;
            }

            string url;
            int listIndex;

            while (true)
            {
                Console.WriteLine("\nProvide a YouTube playlist URL:");
                url = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(url))
                {
                    Console.WriteLine("\nInvalid URL\n");
                    continue;
                }

                listIndex = url.IndexOf(LIST_URL) + LIST_URL.Length;

                if (listIndex == -1)
                {
                    Console.WriteLine("\nInvalid URL\n");
                    continue;
                }

                break;
            }

            var tempDirectory = Directory.CreateTempSubdirectory("download");

            // use yt-dlp to download from youtube

            var ytDLP = new Process();
            ytDLP.StartInfo.FileName = dlpPath;
            ytDLP.StartInfo.Arguments = $"\"{url}\" -P \"{tempDirectory}\" -f bestaudio --force-overwrites --yes-playlist --no-write-comments --write-description --write-playlist-metafiles";
            ytDLP.Start();
            ytDLP.WaitForExit();
            ytDLP.Dispose();

            //

            var tempFiles = new TempFileBundle(tempDirectory);

            PlaylistBundle playlistBundle = GetPlaylistInfo(url, tempFiles);

            Console.WriteLine($"Creating Playlist \"{playlistBundle.Name}\"");
            var finalDirectory = Directory.CreateDirectory(Path.Combine(baseDirectory.FullName, playlistBundle.Name, "tracks"));

            FormatAndPlaceAudio(playlistBundle, tempFiles, finalDirectory, ffmpegPath);
            tempFiles.Dispose();

            CreatePlaylistFiles(playlistBundle, finalDirectory);

            Console.WriteLine("\n\nPlaylist creation complete.\n\n");
        }

        public static void Append(string dlpPath, string ffmpegPath)
        {
            string folder;
            DirectoryInfo playlistDirectory;

            while (true)
            {
                Console.WriteLine("Provide existing playlist directory.\nIt should have playlist file and a 'tracks' folder inside.");
                folder = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(folder))
                {
                    continue;
                }

                try
                {
                    folder = Path.GetFullPath(folder);
                }
                catch
                {
                    Console.WriteLine($"{folder} is not a valid directory.");
                    continue;
                }

                if (!Directory.Exists(Path.Combine(folder, "tracks")))
                {
                    Console.WriteLine("Playlist directory does not have a 'tracks' directory inside.");
                    continue;
                }

                playlistDirectory = Directory.CreateDirectory(folder);

                if (IsInsideProject(playlistDirectory))
                {
                    Console.WriteLine("Cannot open here.");
                    continue;
                }

                break;
            }

            string url;
            while (true)
            {
                Console.WriteLine("Provide YT playlist URL.");
                url = Console.ReadLine();

                if (!string.IsNullOrWhiteSpace(url))
                {
                    break;
                }
            }

            var trackDirectory = Directory.CreateDirectory(Path.Combine(playlistDirectory.FullName, "tracks"));

            var tracks = trackDirectory.EnumerateFiles();

            List<string> ids = new();
            StringBuilder idFilter = new("--match-filter ");
            foreach (var track in tracks)
            {
                var tagFile = TagLib.File.Create(track.FullName);

                Tag idTag = (Tag)tagFile.GetTag(TagLib.TagTypes.Id3v2); // for reading the youtube id
                PrivateFrame p = PrivateFrame.Get(idTag, "yt-id", false);

                string id = Encoding.Unicode.GetString(p.PrivateData.Data);
                ids.Add(id);
                idFilter.Append("id!=");
                idFilter.Append(id);
                idFilter.Append('&');
            }

            string filter = idFilter.ToString()[..^1];

            // use yt-dlp to download from youtube
            var tempDirectory = Directory.CreateTempSubdirectory("download");

            var ytDLP = new Process();
            ytDLP.StartInfo.FileName = dlpPath;
            ytDLP.StartInfo.Arguments = $"\"{url}\" -P \"{tempDirectory}\" -f bestaudio --force-overwrites --yes-playlist --no-write-comments --write-description --write-playlist-metafiles " + filter;
            ytDLP.Start();
            ytDLP.WaitForExit();
            ytDLP.Dispose();

            TempFileBundle tempFiles = new(tempDirectory);
            PlaylistBundle playlistBundle = GetPlaylistInfo(url, tempFiles);

            FormatAndPlaceAudio(playlistBundle, tempFiles, trackDirectory, ffmpegPath);
            tempFiles.Dispose();

            CreatePlaylistFiles(playlistBundle, trackDirectory);

            Console.WriteLine("\n\nAppend Playlist Complete\n\n");
        }

        private static readonly string ORIGINAL_DESCRIPTION_TAG = "\n--- ORIGINAL DESCRIPTION ---\n";

        private static void FormatAndPlaceAudio(PlaylistBundle playlist, TempFileBundle tempFiles, DirectoryInfo finalDirectory, string ffmpegPath)
        {
            int trackCount = 0;
            long dataLength = 0L;
            TimeSpan totalDuration = TimeSpan.Zero;

            MetadataFiller metadataFiller = new();

            foreach (var file in finalDirectory.EnumerateFiles())
            {
                var tagFile = TagLib.File.Create(file.FullName);

                Console.WriteLine();
                Console.WriteLine(file.FullName);
                Console.WriteLine("Grabbing existing file.");
                Console.WriteLine();

                Tag idTag = (Tag)tagFile.GetTag(TagLib.TagTypes.Id3v2); // for reading the youtube id
                PrivateFrame p = PrivateFrame.Get(idTag, "yt-id", false);

                string id;
                if (p != null)
                {
                    id = Encoding.Unicode.GetString(p.PrivateData.Data);
                }
                else
                {
                    id = "";
                }

                string description = tagFile.Tag.Description;
                string originalDescription = description[(description.IndexOf(ORIGINAL_DESCRIPTION_TAG) + ORIGINAL_DESCRIPTION_TAG.Length)..];

                var bundle = new MusicBundle(file, id, file.Name[..^".mp3".Length], originalDescription);
                metadataFiller.Fill(bundle, tagFile);

                var duration = tagFile.Properties.Duration;
                totalDuration += duration;

                dataLength += bundle.File.Length;
                trackCount++;

                tagFile.Tag.Length = duration.ToString(@"hh\:mm\:ss");

                tagFile.Save();
                tagFile.Dispose();
            }

            ConversionHandeler conversion = new(tempFiles.AudioFiles, finalDirectory.FullName, ffmpegPath);
            conversion.Convert();
            var bundles = conversion.GetMusicBundles();

            foreach (var bundle in bundles)
            {
                Console.WriteLine();
                Console.WriteLine($"\n{bundle.File.FullName}");

                Console.WriteLine($"Formatting '{bundle.Title}' ; ID: '{bundle.ID}'");

                dataLength += bundle.File.Length;

                var tagFile = TagLib.File.Create(bundle.File.FullName, TagLib.ReadStyle.Average);
                tagFile.Tag.DateTagged = DateTime.Now;

                // youtube description

                var matches = tempFiles.DescriptionFiles.Where(f => f.Name.Contains($"[{bundle.ID}]"));

                string description = $"Created from a YouTube music playlist.\n" +
                $"https://youtube.com/watch?v={bundle.ID}\n" +
                $"https://youtube.com/playlist?list={playlist.ID}";

                string originalDescription = "";

                if (!matches.Any())
                {
                    Console.WriteLine($"No description found for \"{bundle.Title}\" giving defaults.");
                }
                else
                {
                    using var stream = matches.First().OpenText();
                    originalDescription = stream.ReadToEnd();
                    if (!string.IsNullOrEmpty(originalDescription))
                    {
                        description += ORIGINAL_DESCRIPTION_TAG + originalDescription;
                    }
                }

                bundle.Description = originalDescription;

                metadataFiller.Fill(bundle, tagFile);
                tagFile.Tag.Description = description;

                var duration = tagFile.Properties.Duration;
                totalDuration += duration;

                tagFile.Tag.Length = duration.ToString(@"hh\:mm\:ss");

                trackCount++;

                Tag idTag = (Tag)tagFile.GetTag(TagLib.TagTypes.Id3v2); // for saving the youtube id
                PrivateFrame p = PrivateFrame.Get(idTag, "yt-id", true);
                p.PrivateData = Encoding.Unicode.GetBytes(bundle.ID);

                tagFile.Save();
                tagFile.Dispose();
            }

            using StreamWriter writer = new(Path.Combine(finalDirectory.Parent.FullName, "description.txt"));

            writer.WriteLine($"Name: {playlist.Name}");
            writer.WriteLine($"ID: {playlist.ID}");
            writer.WriteLine("\nDescription:");
            writer.WriteLine(playlist.Description);
            writer.WriteLine("\n");
            writer.WriteLine($"Playlist sourced from https://www.youtube.com/playlist?list={playlist.ID}");
            writer.WriteLine("DO NOT MODIFY THIS FILE");
            writer.WriteLine("\n");
            writer.WriteLine("Stats:");
            writer.WriteLine($"Track count: {trackCount}");
            writer.WriteLine($"File size: {dataLength / 1000} KB");
            writer.WriteLine($"Playlist duration: {totalDuration:hh\\:mm\\:ss}");
        }

        public static string GetNameWithoutURLTag(string name)
        {
            return name[..name.LastIndexOf('[')].Trim();
        }

        public static string GetURLTag(string name)
        {
            return name[(name.LastIndexOf('[') + 1)..name.LastIndexOf(']')];
        }

        private static PlaylistBundle GetPlaylistInfo(string playlistURL, TempFileBundle tempFiles)
        {
            string playlistName, playlistDescription;

            string playlistID;
            if (playlistURL.Contains('&'))
            {
                playlistID = playlistURL.Split('&').Where(s => s.StartsWith(LIST_URL)).First()[LIST_URL.Length..];
            }
            else
            {
                int listIndex = playlistURL.IndexOf(LIST_URL) + LIST_URL.Length;
                playlistID = playlistURL[listIndex..];
            }

            var matches = tempFiles.DescriptionFiles.Where(f => f.Name.Contains($"[{playlistID}]"));

            if (!matches.Any())
            {
                Console.WriteLine("No name found giving default name.");
                playlistName = $"Playlist [{playlistID}]";
                playlistDescription = "";
            }
            else
            {
                var file = matches.First();

                playlistName = GetNameWithoutURLTag(file.Name);
                var invalids = Path.GetInvalidFileNameChars();
                playlistName = string.Join("_", playlistName.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');

                using var stream = matches.First().OpenText();
                playlistDescription = stream.ReadToEnd();
            }

            return new PlaylistBundle(playlistName, playlistDescription, playlistID);
        }

        public static void CreatePlaylistFiles(PlaylistBundle playlistBundle, DirectoryInfo trackDirectory)
        {
            PlaylistFileBuilder gen = new()
            {
                PlaylistBundle = playlistBundle,
                TrackDirectory = trackDirectory,
            };

            gen.Build(trackDirectory.Parent);

            string url = $"https://www.youtube.com/playlist?list={playlistBundle.ID}";
            Console.WriteLine(url);
            BatchAppendMaker.Create(trackDirectory.Parent, url);

            using StreamWriter writer = new(Path.Combine(trackDirectory.Parent.FullName, "README.txt"));
            writer.Write(Resources.readmeText);
        }

        public static bool IsInsideProject(DirectoryInfo targetDirectory)
        {
            var project = new FileInfo(Environment.ProcessPath).Directory;
            return IsInsideProject(targetDirectory, project);
        }

        private static bool IsInsideProject(DirectoryInfo targetDirectory, DirectoryInfo project)
        {
            if (targetDirectory.FullName == project.FullName)
            {
                return true;
            }

            var parent = targetDirectory.Parent;
            if (parent == null)
            {
                return false;
            }

            return IsInsideProject(parent, project);
        }

        private class TempFileBundle
        {
            private readonly IEnumerable<FileInfo> descriptionFiles;
            private readonly IEnumerable<FileInfo> audioFiles;
            private readonly IEnumerable<FileInfo> allFiles;

            private bool dead = false;

            public TempFileBundle(DirectoryInfo tempDir)
            {
                allFiles = tempDir.EnumerateFiles();
                descriptionFiles = allFiles.Where(f => f.Name.EndsWith(".description"));
                audioFiles = allFiles.Where(f => !f.Name.EndsWith(".description"));
            }

            public IEnumerable<FileInfo> AudioFiles
            {
                get { if (dead) throw new ObjectDisposedException("tempfiles"); return audioFiles; }
            }

            public IEnumerable<FileInfo> AllFiles
            {
                get { if (dead) throw new ObjectDisposedException("tempfiles"); return allFiles; }
            }

            public IEnumerable<FileInfo> DescriptionFiles
            {
                get { if (dead) throw new ObjectDisposedException("tempfiles"); return descriptionFiles; }
            }

            public void Dispose()
            {
                allFiles.First().Directory.Delete(true);
                dead = true;
            }
        }
    }
}
