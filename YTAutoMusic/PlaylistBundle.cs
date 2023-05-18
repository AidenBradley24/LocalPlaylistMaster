namespace YTAutoMusic
{
    public class PlaylistBundle
    {
        public string Name { get; private set; }
        public string ID { get; private set; }
        public string Description { get; private set; }

        public PlaylistBundle(string name, string description, string id)
        {
            Name = name;
            ID = id;
            Description = description;
        }
    }
}
