namespace YTAutoMusic.Playlist_Files
{
    internal class M3U8_LocalPath : M3U8File
    {
        public override string ConfigName => "M3U8 (local path) playlist";

        protected override string GetLocation(FileInfo file)
        {
            return Path.Combine("tracks", file.Name);
        }
    }
}
