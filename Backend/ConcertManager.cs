namespace LocalPlaylistMaster.Backend
{
    /// <summary>
    /// Single media supporting splitting chapters
    /// </summary>
    public interface IConcertManager
    {
        public Task Initialize();

        /// <summary>
        /// Split concert track into its respective pieces. Orphans the old tracks.
        /// </summary>
        /// <returns></returns>
        public Task SplitAndCreate();
    }

    public record Concert
    {
        public int concertTrackId = -1;
        public List<TrackRecord> trackRecords = [];
        public const string CONCERT_TRACK = "!concert"; // reserved name

        public record TrackRecord
        {
            public TrackRecord(string name, TimeSpan startTime, TimeSpan endTime, int trackId)
            {
                Name = name;
                StartTime = startTime;
                EndTime = endTime;
                TrackId = trackId;
            }

            public string Name { get; set; }
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
            public int TrackId { get; set; }
        }

        public void EnsureNamesAreUnique()
        {
            HashSet<string> names = [CONCERT_TRACK];
            foreach (TrackRecord record in trackRecords)
            {
                string originalName = record.Name;
                string name = record.Name;
                int index = 1;
                while (names.Contains(name))
                {
                    name = originalName + " " + ++index;
                }
                names.Add(name);
                record.Name = name;
            }
        }
    }
}
