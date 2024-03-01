namespace LocalPlaylistMaster.Backend
{
    /// <summary>
    /// A bundle of data related to a remote playlist.
    /// </summary>
    public record Remote
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Link { get; set; }
        public int TrackCount { get; set; }
        public RemoteType Type { get; set; }
        public RemoteSettings Settings { get; set; }

        public const int UNINITIALIZED = -1;

        public Remote(int id, string name, string description, string link, int trackCount, RemoteType type, RemoteSettings settings)
        {
            Id = id;
            Name = name;
            Description = description;
            Link = link;
            TrackCount = trackCount;
            Type = type;
            Settings = settings;
        }

        public Remote()
        {
            Id = UNINITIALIZED;
            Name = "";
            Description = "";
            Link = "";
            Type = RemoteType.UNINITIALIZED;
            Settings = RemoteSettings.none;
        }
    }

    public enum RemoteType 
    { 
        UNINITIALIZED = -1, ytdlp
    }

    [Flags]
    public enum RemoteSettings 
    { 
        none = 0,
        removeMe = 1 << 0, 
        locked = 1 << 1,
    }
}
