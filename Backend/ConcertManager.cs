﻿using LocalPlaylistMaster.Backend.Utilities;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Globalization;
using System.Text;
using static LocalPlaylistMaster.Backend.Utilities.ProgressModel;

namespace LocalPlaylistMaster.Backend
{
    /// <summary>
    /// Single media supporting splitting chapters
    /// </summary>
    public interface IConcertManager
    {
        /// <summary>
        /// Json user with a "concert" element. Usually <see cref="Remote"/>.
        /// </summary>
        public IMiscJsonUser JsonUser { get; }

        /// <summary>
        /// Ensures concert is ready for visualization and <seealso cref="SplitAndCreate"/>.
        /// </summary>
        /// <returns></returns>
        public Task Initialize();

        /// <summary>
        /// Split concert track into its respective pieces. Orphans the old tracks.
        /// </summary>
        /// <returns></returns>
        public Task SplitAndCreate(IProgress<(ReportType type, object report)> reporter);

        public sealed Concert GetConcert()
        {
            return TryGetConcert() ?? throw new Exception("concert was not found!");
        }

        public sealed Concert? TryGetConcert()
        {
            return JsonUser.GetProperty<Concert>("concert");
        }

        public sealed void SetConcert(Concert concert)
        {
            JsonUser.SetProperty("concert", concert);
        }
    }

    public record Concert
    {
        private int concertTrackId = -1;
        private List<TrackRecord> trackRecords = [];
        public const string CONCERT_TRACK = "!concert"; // reserved name

        public int ConcertTrackId { get => concertTrackId; set => concertTrackId = value; }
        public List<TrackRecord> TrackRecords { get => trackRecords; set => trackRecords = value; }

        public record TrackRecord : INotifyPropertyChanged
        {
            private TimeSpan startTime;
            private TimeSpan endTime;
            private int trackId;

            public TrackRecord(string name, TimeSpan startTime, TimeSpan endTime, int trackId)
            {
                Name = name;
                StartTime = startTime;
                EndTime = endTime;
                TrackId = trackId;
            }

            public string Name { get; set; }
            [JsonIgnore] public TimeSpan StartTime
            {
                get => startTime; 
                set
                {
                    startTime = value;
                    OnPropertyChanged(nameof(StartTime));
                }
            }
            [JsonIgnore] public TimeSpan EndTime
            {
                get => endTime; 
                set
                {
                    endTime = value;
                    OnPropertyChanged(nameof(StartTime));
                }
            }
            public int TrackId
            {
                get => trackId; 
                set
                {
                    trackId = value;
                    OnPropertyChanged(nameof(TrackId));
                }
            }

            public double Start { get => StartTime.TotalSeconds; set => StartTime = TimeSpan.FromSeconds(value); }
            public double End { get => EndTime.TotalSeconds; set => EndTime = TimeSpan.FromSeconds(value); }

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            public event PropertyChangedEventHandler? PropertyChanged;
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

        /// <summary>
        /// Imports track records from a stream.
        /// h:mm:ss or m:ss timestamp followed by a - and then the name to the newline.
        /// (NO END TIMESTAMP)
        /// </summary>
        /// <param name="text"></param>
        public void Import(Stream stream, TimeSpan concertLength, bool overwrite = true)
        {
            int i = 1;
            if (overwrite)
            {
                TrackRecords.Clear();          
            }
            else
            {
                i = TrackRecords.Count + 1;
            }

            using var reader = new StreamReader(stream);
            TrackRecord? workingRecord = null;

            while (!reader.EndOfStream)
            {
                string? line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) return;
                line = line.Replace('–', '-');
                int index = line.IndexOf('-');
                if (index < 0) continue;
                string timeString = line[..index].Trim();
                TimeSpan time = Timestamps.ParseTime(timeString);
                string name = line[(index+1)..].Trim();
                if(workingRecord != null)
                {
                    workingRecord.EndTime = time;
                }
                workingRecord = new TrackRecord(name, time, time, i++);
                TrackRecords.Add(workingRecord);
            }
            if(workingRecord != null)
            {
                workingRecord.EndTime = concertLength;
            }

            EnsureNamesAreUnique();
        }
    }
}
