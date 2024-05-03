using System.Text.RegularExpressions;

namespace LocalPlaylistMaster.Backend
{
    public partial record Playlist
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Tracks { get; set; }

        public int UNINITIALIZED = -1;

        public Playlist()
        {
            Id = UNINITIALIZED;
            Name = "";
            Description = "";
            Tracks = "";
        }

        public Playlist(int id, string name, string description, string tracks)
        {
            Id = id;
            Name = name;
            Description = description;
            Tracks = tracks;
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
    }
}
