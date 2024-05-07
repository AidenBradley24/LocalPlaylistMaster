using System.Text.RegularExpressions;
using LocalPlaylistMaster.Backend.Utilities;

namespace LocalPlaylistMaster.Backend
{
    /// <summary>
    /// A bundle of data related to a remote playlist.
    /// </summary>
    public partial record Remote : IMiscJsonUser
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Link { get; set; }
        public int TrackCount { get; set; }
        public RemoteType Type { get; set; }
        public RemoteSettings Settings { get; set; }
        public string MiscJson { get; set; }

        public bool Locked
        {
            get => Settings.HasFlag(RemoteSettings.locked);
            set
            {
                if (value)
                {
                    Settings |= RemoteSettings.locked;
                }
                else
                {
                    Settings &= ~RemoteSettings.locked;
                }
            }
        }

        public const int UNINITIALIZED = -1;

        public Remote(int id, string name, string description, string link, int trackCount, RemoteType type, RemoteSettings settings, string miscJson)
        {
            Id = id;
            Name = name;
            Description = description;
            Link = link;
            TrackCount = trackCount;
            Type = type;
            Settings = settings;
            MiscJson = miscJson;
        }

        public Remote()
        {
            Id = UNINITIALIZED;
            Name = "";
            Description = "";
            Link = "";
            Type = RemoteType.UNINITIALIZED;
            Settings = RemoteSettings.none;
            MiscJson = "{}";
        }

        public string TruncatedDescription
        {
            get
            {
                string truncated = Description.Length > 100 ? Description[..97] + "..." : Description;
                return WhiteSpace().Replace(truncated, " ");
            }
        }

        [GeneratedRegex("\\s+")]
        private static partial Regex WhiteSpace();

        public bool IsConcert => Type == RemoteType.ytdlp_concert;
    }

    [Flags]
    public enum RemoteSettings 
    { 
        none = 0,
        removeMe = 1 << 0, 
        locked = 1 << 1,
    }
}
