using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Data.SQLite;
using System.Diagnostics;

namespace LocalPlaylistMaster.Backend
{
    /// <summary>
    /// Manages all things playlist
    /// </summary>
    public class PlaylistManager
    {
        public PlaylistRecord Playlist { get; private set; }
        public Status MyStatus { get; private set; }

        private readonly SQLiteConnection db;
        private readonly DependencyProcessManager dependencyProcessManager;

        public PlaylistManager(string folderPath, DependencyProcessManager dependencies)
        {
            dependencyProcessManager = dependencies;
            Directory.CreateDirectory(folderPath);

            string dbPath = Path.Join(folderPath, "playlist.db");
            SQLiteConnection.CreateFile(dbPath);
            db = new SQLiteConnection($"Data Source={dbPath}");
            db.Open();

            string hostFile = Path.Join(folderPath, "host");
            if (File.Exists(hostFile))
            {
                XmlSerializer serializer = new(typeof(PlaylistRecord));
                using FileStream stream = new(hostFile, FileMode.Open);
                Playlist = (PlaylistRecord)serializer.Deserialize(stream);
            }
            else
            {
                Playlist = new();
                XmlSerializer serializer = new(typeof(PlaylistRecord));
                using FileStream stream = new(hostFile, FileMode.Create);
                serializer.Serialize(stream, Playlist);

                using (SQLiteCommand command = db.CreateCommand())
                {
                    command.CommandText = SQL_Resources.create_remotes;
                    command.ExecuteNonQuery();
                }

                using (SQLiteCommand command = db.CreateCommand())
                {
                    command.CommandText = SQL_Resources.create_tracks;
                    command.ExecuteNonQuery();
                }
            }

            MyStatus = Status.ready;
        }

        ~PlaylistManager()
        {
            db?.Close();
        }

        public enum Status { ready, error, searching }

        private async Task Injest(IEnumerable<Track> tracks) // TODO use a transaction
        {
            using SQLiteCommand command = db.CreateCommand();
            StringBuilder sb = new("INSERT INTO Tracks (Name, Remote, RemoteId, Artists," +
                " Album, Description, TimeInSeconds) VALUES\n");

            var e = tracks.GetEnumerator();

            for (int i = 1; true; i++)
            {
                string n = $"@N{i}";
                string r = $"@R{i}";
                string rid = $"@RID{i}";
                string art = $"@ART{i}";
                string des = $"@DES{i}";
                string tme = $"@TME{i}";
                sb.Append($"({n},{r},{rid},{art},{des},{tme})");

                Track track = e.Current;
                bool end = !e.MoveNext();
                if (!end) sb.Append(',');

                SQLiteParameter param;
                param = new SQLiteParameter(n, track.Name);
                command.Parameters.Add(param);
                param = new SQLiteParameter(n, track.Remote);
                command.Parameters.Add(param);
                param = new SQLiteParameter(n, track.RemoteId);
                command.Parameters.Add(param);
                param = new SQLiteParameter(n, track.Artists);
                command.Parameters.Add(param);
                param = new SQLiteParameter(n, track.Album);
                command.Parameters.Add(param);
                param = new SQLiteParameter(n, track.Description);
                command.Parameters.Add(param);
                param = new SQLiteParameter(n, track.TimeInSeconds);
                command.Parameters.Add(param);

                if (end) break;
            }

            string query = sb.ToString();
            command.CommandText = query;
            await command.ExecuteNonQueryAsync();
        }

        public async Task FetchAllRemotes() // TODO instead of a removeme boolean, have a integer flags to represent multiple settings
        {

        }
    }
}
