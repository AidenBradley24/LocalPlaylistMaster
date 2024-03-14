namespace LocalPlaylistMaster.Backend
{
    public record Playlist
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<int> Tracks { get; set; }

        public int UNINITIALIZED = -1;

        public Playlist()
        {
            Id = UNINITIALIZED;
            Name = "";
            Description = "";
            Tracks = new List<int>();
        }

        public Playlist(int id, string name, string description, string tracks)
        {
            Id = id;
            Name = name;
            Description = description;
            SetTracksString(tracks);
        }

        public void SetTracksString(string tracks)
        {
            Tracks = tracks.Split(',').Select(int.Parse).ToList();
        }

        public string GetTracksString()
        {
            return string.Join(',', Tracks);
        }
    }
}
