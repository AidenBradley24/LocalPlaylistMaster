namespace LocalPlaylistMaster.Backend.Playlist_Files
{
    internal class M3U8_AbsolutePath : M3U8File
    {
        protected override string GetLocation(FileInfo file)
        {
            return file.FullName;
        }
    }
}
