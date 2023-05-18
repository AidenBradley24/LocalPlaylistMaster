using System.Text.Encodings.Web;

namespace YTAutoMusic.Playlist_Files
{
    internal class M3U8_URL : M3U8File
    {
        public override string ConfigName => "M3U8 (url) playlist";

        protected override string GetLocation(FileInfo file)
        {
            var url = UrlEncoder.Default;
            return "file:///tracks/" + url.Encode($"{file.Name}");
        }
    }
}
