namespace YTAutoMusic
{
    public class MusicBundle
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public FileInfo File { get; set; }
        public string ID { get; set; }

        public MusicBundle(FileInfo file, string id, string title, string description)
        {
            File = file;
            ID = id;
            Title = title;
            Description = description;
        }
    }
}
