using System.Text.RegularExpressions;

namespace LocalPlaylistMaster.Backend
{
    public partial record Track
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Remote { get; set; }
        public string RemoteId { get; set; }
        public string Artists { get; set; }
        public string Album { get; set; }
        public string Description { get; set; }
        public int Rating { get; set; }
        public int TimeInSeconds { get; set; }

        public TrackSettings Settings { get; set; }
        public bool Locked
        {
            get => Settings.HasFlag(TrackSettings.locked);
            set
            {
                if (value)
                {
                    Settings |= TrackSettings.locked;
                }
                else
                {
                    Settings &= ~TrackSettings.locked;
                }
            }
        }


        public string[] GetArtists()
        {
            return Artists.Split(',');
        }

        public const int UNINITIALIZED = -1;

        public Track(int id, string name, int remote, string remoteId, string artists, string album, string description,
            int rating, int timeInSeconds, TrackSettings settings)
        {
            Id = id;
            Name = name;
            Remote = remote;
            RemoteId = remoteId;
            Artists = artists;
            Album = album;
            Description = description;
            Rating = rating;
            TimeInSeconds = timeInSeconds;
            Settings = settings;
        }

        public Track()
        {
            Id = UNINITIALIZED;
            Name = "";
            Remote = UNINITIALIZED;
            RemoteId = "";
            Artists = "";
            Album = "";
            Description = "";
            Rating = UNINITIALIZED;
            TimeInSeconds = UNINITIALIZED;
        }

        public string LengthString { get => TimeInSeconds == UNINITIALIZED ? "?" : TimeSpan.FromSeconds(TimeInSeconds).ToString(@"hh\:mm\:ss"); }

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
    }

    [Flags]
    public enum TrackSettings
    {
        none = 0,
        removeMe = 1 << 0,
        locked = 1 << 1,
    }
}
