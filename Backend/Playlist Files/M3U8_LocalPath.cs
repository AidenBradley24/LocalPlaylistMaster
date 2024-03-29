namespace LocalPlaylistMaster.Backend.Playlist_Files
{
    internal class M3U8_LocalPath : M3U8File
    {
        protected override string GetLocation(FileInfo file)
        {
            return Path.Combine("tracks", file.Name);
        }
    }
}
