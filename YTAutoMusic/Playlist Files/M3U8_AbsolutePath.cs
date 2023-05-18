namespace YTAutoMusic.Playlist_Files
{
    internal class M3U8_AbsolutePath : M3U8File
    {
        public override string ConfigName => "M3U8 (absolute path) playlist";

        protected override string GetLocation(FileInfo file)
        {
            return file.FullName;
        }
    }
}
