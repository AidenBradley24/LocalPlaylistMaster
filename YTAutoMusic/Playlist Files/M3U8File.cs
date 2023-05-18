using System.Text;

namespace YTAutoMusic.Playlist_Files
{
    internal abstract class M3U8File : PlaylistFile
    {
        public override void Build(DirectoryInfo targetDirectory, DirectoryInfo trackDirectory, PlaylistBundle bundle)
        {
            using FileStream stream = File.Open(Path.Combine(targetDirectory.FullName, "playlist.m3u8"), FileMode.Create);
            using StreamWriter writer = new(stream, Encoding.UTF8);
            writer.WriteLine("#EXTM3U");

            foreach (FileInfo file in trackDirectory.EnumerateFiles())
            {
                string location = GetLocation(file);

                TagLib.File tagFile = TagLib.File.Create(file.FullName);
                string artist = string.Join(" & ", tagFile.Tag.Performers).Replace(",", "").Replace("-", "|").Trim();
                string title = tagFile.Tag.Title.Replace(",", "").Replace("-", "|").Trim();

                writer.WriteLine($"#EXTINF:{tagFile.Properties.Duration.Seconds},{artist} - {title}");
                writer.WriteLine(location);
            }
        }

        protected abstract string GetLocation(FileInfo file);
    }
}
