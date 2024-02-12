using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Data.SQLite;
using System.Diagnostics;
using System.Data;

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
                " Album, Description, TimeInSeconds, Settings) VALUES\n");

            var e = tracks.GetEnumerator();

            for (int i = 1; true; i++)
            {
                string n = $"@N{i}";
                string r = $"@R{i}";
                string rid = $"@RID{i}";
                string art = $"@ART{i}";
                string alb = $"@ALB{i}";
                string des = $"@DES{i}";
                string tme = $"@TME{i}";
                string set = $"@SET{i}";
                sb.Append($"({n},{r},{rid},{art},{alb},{des},{tme},{set})");

                Track track = e.Current;
                bool end = !e.MoveNext();
                if (!end) sb.Append(',');

                SQLiteParameter param;
                param = new SQLiteParameter(n, track.Name);
                command.Parameters.Add(param);
                param = new SQLiteParameter(r, track.Remote);
                command.Parameters.Add(param);
                param = new SQLiteParameter(rid, track.RemoteId);
                command.Parameters.Add(param);
                param = new SQLiteParameter(art, track.Artists);
                command.Parameters.Add(param);
                param = new SQLiteParameter(alb, track.Album);
                command.Parameters.Add(param);
                param = new SQLiteParameter(des, track.Description);
                command.Parameters.Add(param);
                param = new SQLiteParameter(tme, track.TimeInSeconds);
                command.Parameters.Add(param);
                param = new SQLiteParameter(set, track.Settings);
                command.Parameters.Add(param);

                if (end) break;
            }

            string query = sb.ToString();
            command.CommandText = query;
            await command.ExecuteNonQueryAsync();
        }

        public async Task<RemoteManager> GetRemote(int id)
        {
            using SQLiteCommand command = db.CreateCommand();
            command.CommandText = $"SELECT * FROM Remotes WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            string link = reader.GetString("Link");
            RemoteType type = (RemoteType)reader.GetInt32("Type");
            RemoteSettings settings = (RemoteSettings)reader.GetInt32("Settings");

            return type switch
            {
                RemoteType.ytdlp => new YTdlpManager(dependencyProcessManager, settings, link),
                _ => throw new Exception("Invalid remote type")
            };
        }

        public async Task<IEnumerable<Track>> GetTracks(int limit = 10)
        {
            using SQLiteCommand command = db.CreateCommand();
            command.CommandText = $"SELECT * FROM Tracks LIMIT @Limit";
            command.Parameters.AddWithValue("@Limit", limit);

            using var reader = await command.ExecuteReaderAsync();
            List<Track> tracks = new();
            do
            {
                Track track = new(reader.GetInt32("Id"), reader.GetString("Name"),
                    reader.GetInt32("Remote"), reader.GetString("RemoteId"), reader.GetString("Artists"),
                    reader.GetString("Album"), reader.GetString("Description"), reader.GetInt32("Rating"),
                    reader.GetInt32("TimeInSeconds"), (TrackSettings)reader.GetInt32("Settings"));
            } while (await reader.NextResultAsync());

            return tracks;
        }

        public async Task FetchRemote(int remote)
        {
            RemoteManager manager = await GetRemote(remote);
            (string playlistName, string playlistDescription, IEnumerable<Track> tracks)
                = await manager.FetchRemote();

            int count = await UpdateTracksWithRemote(tracks, remote);
        }

        private async Task UpdateRemote(RemoteSettings settings, string remoteName, string remoteDescription, int trackCount)
        {
            if (!settings.HasFlag(RemoteSettings.locked))
            {

            }
        }

        private async Task<int> UpdateTracksWithRemote(IEnumerable<Track> tracks, int remote)
        {
            int updatedTrackCount = 0;

            // remove remote from orphaned tracks
            using (SQLiteCommand command = db.CreateCommand())
            {
                var idPlaceholders = string.Join(",", tracks.Select((_, index) => $"@Id{index}"));
                var idParameters = tracks.Select((t, index) => new SQLiteParameter($"@Id{index}", t.Id)).ToArray();

                command.CommandText = $"UPDATE Tracks SET Remote = @NewRemote WHERE Remote = @Remote AND Id NOT IN ({idPlaceholders})";
                command.Parameters.AddWithValue("@Remote", remote);
                command.Parameters.AddWithValue("@NewRemote", -1); // TODO make const for no remote
                command.Parameters.AddRange(idParameters);
                updatedTrackCount = await command.ExecuteNonQueryAsync();
            }

            Trace.WriteLine($"Orphaned {updatedTrackCount} remotes with remote #{remote}");
            return updatedTrackCount + await UpdateTracks(tracks);
        }

        public async Task<int> UpdateTracks(IEnumerable<Track> tracks)
        {
            int updatedTrackCount = 0;

            using (SQLiteCommand command = db.CreateCommand())
            {
                StringBuilder sb = new("UPDATE TRACKS SET ");
                sb.Append("Name = @Name, ");
                sb.Append("Remote = @Remote, ");
                sb.Append("RemoteId = @RemoteId, ");
                sb.Append("Artists = @Artists, ");
                sb.Append("Album = @Album, ");
                sb.Append("Description = @Description, ");
                sb.Append("Rating = @Rating, ");
                sb.Append("TimeInSeconds = @TimeInSeconds, ");
                sb.Append("Settings = @Settings ");
                sb.Append("WHERE Id = @Id");
                command.CommandText = sb.ToString();

                foreach (Track track in tracks)
                {
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@Name", track.Name);
                    command.Parameters.AddWithValue("@Remote", track.Remote);
                    command.Parameters.AddWithValue("@RemoteId", track.RemoteId);
                    command.Parameters.AddWithValue("@Artists", track.Artists);
                    command.Parameters.AddWithValue("@Album", track.Album);
                    command.Parameters.AddWithValue("@Description", track.Description);
                    command.Parameters.AddWithValue("@Rating", track.Rating);
                    command.Parameters.AddWithValue("@TimeInSeconds", track.TimeInSeconds);
                    command.Parameters.AddWithValue("@Settings", (int)track.Settings);
                    command.Parameters.AddWithValue("@Id", track.Id);

                    updatedTrackCount += await command.ExecuteNonQueryAsync();
                }
            }

            return updatedTrackCount;
        }
    }
}
