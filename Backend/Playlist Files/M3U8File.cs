using System.Text;
using System.Text.Encodings.Web;

namespace LocalPlaylistMaster.Backend.Playlist_Files
{
    internal class M3U8File : PlaylistFile
    {
        public override void Build(DirectoryInfo targetDirectory, Playlist bundle, Dictionary<FileInfo, Track> trackFileMap, bool fullPath)
        {
            using FileStream stream = File.Open(Path.Combine(targetDirectory.FullName, "playlist.m3u8"), FileMode.Create);
            using StreamWriter writer = new(stream, Encoding.UTF8);
            writer.WriteLine("#EXTM3U");

            var url = UrlEncoder.Default;

            foreach(var pair in trackFileMap)
            {
                FileInfo file = pair.Key;
                Track track = pair.Value;

                string location = fullPath ? "file:///" + url.Encode(file.FullName) : "file:///tracks/" + url.Encode(file.Name);
                string artist = string.Join(" & ", track.Artists).Replace(",", "").Replace("-", "|").Trim();
                string title = track.Name.Replace(",", "").Replace("-", "|").Trim();

                writer.WriteLine($"#EXTINF:{track.TimeInSeconds},{artist} - {title}");
                writer.WriteLine(location);
            }
        }
    }
}
