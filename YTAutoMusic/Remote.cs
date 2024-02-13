namespace LocalPlaylistMaster.Backend
{
    /// <summary>
    /// A bundle of data related to a remote playlist.
    /// </summary>
    public record Remote(int Id, string Name, string Description, string Link, int TrackCount, RemoteType Type, RemoteSettings Settings)
    {
        public const int UNINITIALIZED = -1;
    }

    public enum RemoteType { ytdlp }

    [Flags]
    public enum RemoteSettings 
    { 
        none = 0,
        removeMe = 1 << 0, 
        locked = 1 << 1,
    }
}
