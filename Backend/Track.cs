using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text;
using LocalPlaylistMaster.Backend.Utilities;

namespace LocalPlaylistMaster.Backend
{
    public partial record Track : IEqualityComparer<Track>, IComparable<Track>, IMiscJsonUser
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Remote { get; set; }
        public string RemoteId { get; set; }
        public string Artists { get; set; }
        public string Album { get; set; }
        public string Description { get; set; }
        public int Rating { get; set; }
        public double TimeInSeconds { get; set; }
        public string MiscJson { get; set; }

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

        public bool Downloaded
        {
            get => Settings.HasFlag(TrackSettings.downloaded);
            internal set
            {
                if (value)
                {
                    Settings |= TrackSettings.downloaded;
                }
                else
                {
                    Settings &= ~TrackSettings.downloaded;
                }
            }
        }

        public string[] GetArtists()
        {
            return Artists.Split(',');
        }

        public const int UNINITIALIZED = -1;

        public Track(int id, string name, int remote, string remoteId, string artists, string album, string description,
            int rating, double timeInSeconds, TrackSettings settings, string miscJson)
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
            MiscJson = miscJson;
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
            MiscJson = "{}";
        }

        public Track(Track old)
        {
            Id = old.Id;
            Name = old.Name;
            Remote = old.Remote;
            RemoteId = old.RemoteId;
            Artists = old.Artists;
            Album = old.Album;
            Description = old.Description;
            Rating = old.Rating;
            TimeInSeconds = old.TimeInSeconds;
            Settings = old.Settings;
            MiscJson = old.MiscJson;
        }

        public void Backup()
        {
            using var doc = JsonDocument.Parse(MiscJson);
            var root = doc.RootElement;
            JsonObject backupObject = new()
            {
                { nameof(Name), Name },
                { nameof(Description), Description },
                { nameof(Artists), Artists },
                { nameof(Album), Album },
                { nameof(Rating), Rating }
            };
            ((IMiscJsonUser)this).UpdateJson(root, "backup", backupObject);
        }

        public void Rollback()
        {
            using var doc = JsonDocument.Parse(MiscJson);
            var root = doc.RootElement;
            if(root.TryGetProperty("backup", out JsonElement backup))
            {
                Name = backup.GetProperty(nameof(Name)).GetString() ?? Name;
                Description = backup.GetProperty(nameof(Description)).GetString() ?? Description;
                Artists = backup.GetProperty(nameof(Artists)).GetString() ?? Artists;
                Album = backup.GetProperty(nameof(Album)).GetString() ?? Album;
                Rating = backup.GetProperty(nameof(Rating)).GetInt32();
            }
        }

        public (string name, string description, string artists, string album, int rating)? SoftRollback()
        {
            using var doc = JsonDocument.Parse(MiscJson);
            var root = doc.RootElement;
            if (root.TryGetProperty("backup", out JsonElement backup))
            {
                string name = backup.GetProperty(nameof(Name)).GetString() ?? Name;
                string description = backup.GetProperty(nameof(Description)).GetString() ?? Description;
                string artists = backup.GetProperty(nameof(Artists)).GetString() ?? Artists;
                string album = backup.GetProperty(nameof(Album)).GetString() ?? Album;
                int rating = backup.GetProperty(nameof(Rating)).GetInt32();
                return (name, description, artists, album, rating);
            }

            return null;
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

        public bool Equals(Track? x, Track? y)
        {
            return x?.Id == y?.Id;
        }

        public int GetHashCode([DisallowNull] Track obj)
        {
            return obj.Id.GetHashCode();
        }

        public int CompareTo(Track? other)
        {
            return Id.CompareTo(other?.Id);
        }

        public override string ToString()
        {
            return $"#{Id} -- {Name}";
        }

        public TimeSpan Length { get => TimeSpan.FromSeconds(TimeInSeconds); }
    }

    [Flags]
    public enum TrackSettings
    {
        none = 0,
        removeMe = 1 << 0,
        locked = 1 << 1,
        downloaded = 1 << 2,
    }
}
