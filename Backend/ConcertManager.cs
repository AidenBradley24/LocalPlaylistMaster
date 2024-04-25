using System.Text.Json.Serialization;

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
        private int concertTrackId = -1;
        private List<TrackRecord> trackRecords = [];
        public const string CONCERT_TRACK = "!concert"; // reserved name

        public int ConcertTrackId { get => concertTrackId; set => concertTrackId = value; }
        public List<TrackRecord> TrackRecords { get => trackRecords; set => trackRecords = value; }

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
            [JsonIgnore] public TimeSpan StartTime { get; set; }
            [JsonIgnore] public TimeSpan EndTime { get; set; }
            public int TrackId { get; set; }

            public double Start { get => StartTime.TotalSeconds; set => StartTime = TimeSpan.FromSeconds(value); }
            public double End { get => EndTime.TotalSeconds; set => EndTime = TimeSpan.FromSeconds(value); }
        }

        public void EnsureNamesAreUnique()
        {
            HashSet<string> names = [CONCERT_TRACK];
            foreach (TrackRecord record in TrackRecords)
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
