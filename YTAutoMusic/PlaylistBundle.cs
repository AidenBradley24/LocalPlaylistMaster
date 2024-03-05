namespace LocalPlaylistMaster.Backend
{
    public class PlaylistBundle
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<int> Tracks { get; set; }

        public PlaylistBundle(int id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
            Tracks = new List<int>();
        }
    }
}
