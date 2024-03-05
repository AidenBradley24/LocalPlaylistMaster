using System.Text;
using System.Xml.Serialization;
using System.Data.SQLite;
using System.Diagnostics;
using System.Data;
using static LocalPlaylistMaster.Backend.ProgressModel;
using System.Reflection;

namespace LocalPlaylistMaster.Backend
{
    /// <summary>
    /// Manages all things playlist
    /// </summary>
    public sealed class PlaylistManager
    {
        public PlaylistRecord Playlist { get; private set; }
        public Status MyStatus { get; private set; }

        private readonly SQLiteConnection db;
        private readonly DependencyProcessManager dependencyProcessManager;
        private readonly FileInfo hostFile;

        private int trackCount = -1;
        private int remoteCount = -1;

        public PlaylistManager(string folderPath, DependencyProcessManager dependencies)
        {
            dependencyProcessManager = dependencies;
            Directory.CreateDirectory(folderPath);

            string dbPath = Path.Join(folderPath, "playlist.db");
            db = new SQLiteConnection($"Data Source={dbPath}");

            hostFile = new(Path.Join(folderPath, "host"));
            if (File.Exists(hostFile.FullName))
            {
                if (!File.Exists(dbPath))
                {
                    throw new FileNotFoundException($"'{dbPath}' does not exist");
                }

                db.Open();

                XmlSerializer serializer = new(typeof(PlaylistRecord));
                using FileStream stream = new(hostFile.FullName, FileMode.Open);
                Playlist = serializer.Deserialize(stream) as PlaylistRecord ?? throw new Exception("Invalid host file");
            }
            else
            {
                Playlist = new();
                XmlSerializer serializer = new(typeof(PlaylistRecord));
                using FileStream stream = new(hostFile.FullName, FileMode.Create);
                serializer.Serialize(stream, Playlist);

                SQLiteConnection.CreateFile(dbPath);
                db.Open();

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

        public void UpdatePlaylistRecord()
        {
            XmlSerializer serializer = new(typeof(PlaylistRecord));
            using FileStream stream = new(hostFile.FullName, FileMode.Create);
            serializer.Serialize(stream, Playlist);
        }

        ~PlaylistManager()
        {
            UpdatePlaylistRecord();
            db?.Close();
        }

        public enum Status { ready, error, searching }

        private async Task IngestTracks(IEnumerable<Track> tracks) // TODO use a transaction
        {
            using SQLiteCommand command = db.CreateCommand();
            StringBuilder sb = new("INSERT INTO Tracks (Name, Remote, RemoteId, Artists," +
                " Album, Description, Rating, TimeInSeconds, Settings) VALUES\n");

            var e = tracks.GetEnumerator();
            if (!e.MoveNext()) return;

            for (int i = 1; true; i++)
            {
                string n = $"@N{i}";
                string r = $"@R{i}";
                string rid = $"@RID{i}";
                string art = $"@ART{i}";
                string alb = $"@ALB{i}";
                string des = $"@DES{i}";
                string rat = $"@RAT{i}";
                string tme = $"@TME{i}";
                string set = $"@SET{i}";
                sb.Append($"({n},{r},{rid},{art},{alb},{des},{rat},{tme},{set})");

                Track track = e.Current;
                bool end = !e.MoveNext();
                if (!end) sb.Append(',');

                command.Parameters.AddWithValue(n, track.Name);
                command.Parameters.AddWithValue(r, track.Remote);
                command.Parameters.AddWithValue (rid, track.RemoteId);
                command.Parameters.AddWithValue(art, track.Artists);
                command.Parameters.AddWithValue(alb, track.Album);
                command.Parameters.AddWithValue(des, track.Description);
                command.Parameters.AddWithValue(rat, track.Rating);
                command.Parameters.AddWithValue(tme, track.TimeInSeconds);
                command.Parameters.AddWithValue(set, (int)track.Settings);

                if (end) break;
            }

            string query = sb.ToString();
            command.CommandText = query;
            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Creates a remote manager from an existing remote in the database.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<RemoteManager> GetRemoteManager(int id)
        {
            using SQLiteCommand command = db.CreateCommand();
            command.CommandText = $"SELECT * FROM Remotes WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();

            if(await reader.ReadAsync())
            {
                string link = reader.GetString("Link");
                RemoteType type = (RemoteType)reader.GetInt32("Type");
                RemoteSettings settings = (RemoteSettings)reader.GetInt32("Settings");
                Remote existingRemote = new()
                {
                    Id = id,
                    Link = link,
                    Type = type,
                    Settings = settings
                };
                return type switch
                {
                    RemoteType.ytdlp => new YTdlpManager(existingRemote, dependencyProcessManager),
                    _ => throw new Exception("Invalid remote type")
                };
            }
            else
            {
                throw new Exception("Remote was not found in the database!");
            }
        }

        public async Task<IEnumerable<Track>> GetTracks(int limit, int offset)
        {
            using SQLiteCommand command = db.CreateCommand();
            command.CommandText = $"SELECT * FROM Tracks LIMIT @Limit OFFSET @Offset";
            command.Parameters.AddWithValue("@Limit", limit);
            command.Parameters.AddWithValue("@Offset", offset);

            using var reader = await command.ExecuteReaderAsync();
            List<Track> tracks = new();

            while (await reader.ReadAsync())
            {
                Track track = new(
                    reader.GetInt32("Id"),
                    reader.GetString("Name"),
                    reader.GetInt32("Remote"),
                    reader.GetString("RemoteId"),
                    reader.GetString("Artists"),
                    reader.GetString("Album"),
                    reader.GetString("Description"),
                    reader.GetInt32("Rating"),
                    reader.GetInt32("TimeInSeconds"),
                    (TrackSettings)reader.GetInt32("Settings"));
                tracks.Add(track);
            }

            return tracks;
        }

        public async Task<IEnumerable<Remote>> GetRemotes(int limit, int offset)
        {
            using SQLiteCommand command = db.CreateCommand();
            command.CommandText = $"SELECT * FROM Remotes LIMIT @Limit OFFSET @Offset";
            command.Parameters.AddWithValue("@Limit", limit);
            command.Parameters.AddWithValue("@Offset", offset);

            using var reader = await command.ExecuteReaderAsync();
            List<Remote> remotes = new();

            while (await reader.ReadAsync())
            {
                Remote track = new(
                    reader.GetInt32("Id"),
                    reader.GetString("Name"),
                    reader.GetString("Description"),
                    reader.GetString("Link"),
                    reader.GetInt32("TrackCount"),
                    (RemoteType)reader.GetInt32("Type"),
                    (RemoteSettings)reader.GetInt32("Settings"));
                remotes.Add(track);
            }

            return remotes;
        }

        /// <summary>
        /// Fetch and update an existing remote and its tracks
        /// </summary>
        /// <param name="remote"></param>
        /// <returns></returns>
        public async Task FetchRemote(int remote, IProgress<(ReportType type, object report)> reporter)
        {
            using SQLiteTransaction transaction = db.BeginTransaction();
            try
            {
                reporter.Report((ReportType.TitleText, "Fetching Remote"));
                reporter.Report((ReportType.DetailText, "pulling from database"));
                RemoteManager manager = await GetRemoteManager(remote);
                reporter.Report((ReportType.DetailText, "fetching remote"));
                (Remote fetchedRemote, IEnumerable<Track> fetchedTracks) = await manager.FetchRemote(reporter);
                reporter.Report((ReportType.DetailText, "updating database"));
                await UpdateTracksRemote(fetchedTracks, remote);
                (var existingTracks, var newTracks) = await FilterExistingTracks(fetchedTracks);
                await IngestTracks(newTracks);
                var unlockedTracks = await FilterUnlockedTracks(existingTracks);
                await UpdateRemote(fetchedRemote, manager.ExistingRemote);
                await UpdateTracks(unlockedTracks);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            await transaction.CommitAsync();
            InvalidateTrackCount();
            InvalidateRemoteCount();
        }

        public async Task<int> IngestRemote(Remote remote)
        {
            using SQLiteTransaction transaction = db.BeginTransaction();
            try
            {
                using SQLiteCommand command = db.CreateCommand();
                command.CommandText = "INSERT INTO Remotes (Name, Description, Link, TrackCount, Type, Settings) " +
                    "VALUES (@Name, @Description, @Link, @TrackCount, @Type, @Settings); " +
                    "SELECT LAST_INSERT_ROWID();";

                command.Parameters.AddWithValue("@Name", remote.Name);
                command.Parameters.AddWithValue("@Description", remote.Description);
                command.Parameters.AddWithValue("@Link", remote.Link);
                command.Parameters.AddWithValue("@TrackCount", 0);
                command.Parameters.AddWithValue("@Type", (int)remote.Type);
                command.Parameters.AddWithValue("@Settings", (int)remote.Settings);
                object? id = await command.ExecuteScalarAsync();
                await transaction.CommitAsync();
                return Convert.ToInt32(id);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

        }

        /// <summary>
        /// Syncs a fetched remote with the existing remote on the database
        /// </summary>
        /// <param name="fetchedRemote"></param>
        /// <param name="existingRemote"></param>
        /// <returns></returns>
        private async Task UpdateRemote(Remote fetchedRemote, Remote existingRemote)
        {
            Trace.Assert(fetchedRemote.Id == existingRemote.Id);

            using SQLiteCommand command = db.CreateCommand();
            command.CommandText = "UPDATE Remotes SET TrackCount = @Count";
            command.Parameters.AddWithValue("@Count", fetchedRemote.TrackCount);

            if (!existingRemote.Settings.HasFlag(RemoteSettings.locked))
            {
                command.CommandText += ", Name = @Name, Description = @Description";
                command.Parameters.AddWithValue("@Name", fetchedRemote.Name);
                command.Parameters.AddWithValue("@Description", fetchedRemote.Description);
            }

            command.CommandText += " WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", existingRemote.Id);
            await command.ExecuteNonQueryAsync();
        }

        private async Task<int> UpdateTracksRemote(IEnumerable<Track> fetchedTracks, int remote)
        {
            int updatedTrackCount = 0;

            // remove remote from orphaned tracks
            using (SQLiteCommand command = db.CreateCommand())
            {
                var idPlaceholders = string.Join(",", fetchedTracks.Select((_, index) => $"@Id{index}"));
                var idParameters = fetchedTracks.Select((t, index) => new SQLiteParameter($"@Id{index}", t.Id)).ToArray();

                command.CommandText = $"UPDATE Tracks SET Remote = @NewRemote WHERE Remote = @Remote AND Id NOT IN ({idPlaceholders})";
                command.Parameters.AddWithValue("@Remote", remote);
                command.Parameters.AddWithValue("@NewRemote", -1); // TODO make const for no remote
                command.Parameters.AddRange(idParameters);
                updatedTrackCount = await command.ExecuteNonQueryAsync();
            }

            Trace.WriteLine($"Orphaned {updatedTrackCount} remotes with remote #{remote}");
            return updatedTrackCount + await UpdateTracks(fetchedTracks);
        }

        /// <summary>
        /// Filter out tracks which are locked in the database.
        /// Tracks must already be in database
        /// </summary>
        /// <param name="tracks">Tracks to check</param>
        /// <returns>Collection of unlocked tracks</returns>
        private async Task<IEnumerable<Track>> FilterUnlockedTracks(IEnumerable<Track> tracks)
        {
            HashSet<int> unlocked = new();
            using SQLiteCommand command = db.CreateCommand();
            command.CommandText = "SELECT Id FROM Tracks WHERE (Settings & @Flag) != 0";
            command.Parameters.AddWithValue("@Flag", TrackSettings.locked);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                unlocked.Add(reader.GetInt32("Id"));
            }

            return tracks.Where(t => unlocked.Contains(t.Id));
        }

        /// <summary>
        /// Update existing tracks with new information. Ignores the lock setting. DOES NOT SET REMOTE
        /// </summary>
        /// <param name="tracks">Collection of tracks</param>
        /// <returns>Number of updates</returns>
        public async Task<int> UpdateTracks(IEnumerable<Track> tracks)
        {
            int updatedCount = 0;

            using (SQLiteCommand command = db.CreateCommand())
            {
                StringBuilder sb = new("UPDATE Tracks SET ");
                sb.Append("Name = @Name, ");
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
                    command.Parameters.AddWithValue("@RemoteId", track.RemoteId);
                    command.Parameters.AddWithValue("@Artists", track.Artists);
                    command.Parameters.AddWithValue("@Album", track.Album);
                    command.Parameters.AddWithValue("@Description", track.Description);
                    command.Parameters.AddWithValue("@Rating", track.Rating);
                    command.Parameters.AddWithValue("@TimeInSeconds", track.TimeInSeconds);
                    command.Parameters.AddWithValue("@Settings", (int)track.Settings);
                    command.Parameters.AddWithValue("@Id", track.Id);

                    updatedCount += await command.ExecuteNonQueryAsync();
                }
            }

            return updatedCount;
        }

        /// <summary>
        /// Update existing remotes with new information. Ignores the lock setting.
        /// </summary>
        /// <param name="remotes">Collection of remotes</param>
        /// <returns>Number of updates</returns>
        public async Task<int> UpdateRemotes(IEnumerable<Remote> remotes)
        {
            int updatedCount = 0;

            using (SQLiteCommand command = db.CreateCommand())
            {
                StringBuilder sb = new("UPDATE Remotes SET ");
                sb.Append("Name = @Name, ");
                sb.Append("Description = @Description, ");
                sb.Append("Link = @Link, ");
                sb.Append("TrackCount = @TrackCount, ");
                sb.Append("Settings = @Settings ");
                sb.Append("WHERE Id = @Id");
                command.CommandText = sb.ToString();

                foreach (Remote remote in remotes)
                {
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@Name", remote.Name);
                    command.Parameters.AddWithValue("@Description", remote.Description);
                    command.Parameters.AddWithValue("@Link", remote.Link);
                    command.Parameters.AddWithValue("@TrackCount", remote.TrackCount);
                    command.Parameters.AddWithValue("@Settings", (int)remote.Settings);
                    command.Parameters.AddWithValue("@Id", remote.Id);

                    updatedCount += await command.ExecuteNonQueryAsync();
                }
            }

            return updatedCount;
        }

        private async Task<(IEnumerable<Track> existingTracks, IEnumerable<Track> newTracks)> FilterExistingTracks(IEnumerable<Track> allTracks)
        {
            List<Track> existingTracks = new();
            List<Track> newTracks = new();

            using (SQLiteCommand command = db.CreateCommand())
            {
                command.CommandText = "SELECT Id FROM Tracks WHERE Id = @Id";
                command.Parameters.Add("@Id", DbType.Int32);
                foreach (Track track in allTracks)
                {
                    command.Parameters["@Id"].Value = track.Id;
                    object? result = await command.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                    {
                        existingTracks.Add(track);
                    }
                    else
                    {
                        newTracks.Add(track);
                    }
                }
            }

            return (existingTracks, newTracks);
        }

        public int GetTrackCount()
        {
            if(trackCount >= 0) return trackCount;

            using SQLiteCommand command = db.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Tracks";
            trackCount = Convert.ToInt32(command.ExecuteScalar());
            return trackCount;
        }

        public int GetRemoteCount()
        {
            if (remoteCount >= 0) return remoteCount;

            using SQLiteCommand command = db.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Remotes";
            remoteCount = Convert.ToInt32(command.ExecuteScalar());
            return remoteCount;
        }

        private void InvalidateTrackCount()
        {
            trackCount = -1;
        }

        private void InvalidateRemoteCount()
        {
            remoteCount = -1;
        }

        public async Task<Dictionary<int, string>> GetRemoteNames(IEnumerable<int> remoteIDs)
        {
            Dictionary<int, string> remoteMap = new();
            using SQLiteCommand command = db.CreateCommand();
            command.CommandText = "SELECT Id, Name FROM Remotes WHERE Id IN (" + string.Join(",", remoteIDs) + ")";
            using var reader = await command.ExecuteReaderAsync();              
            while (await reader.ReadAsync())
            {
                int id = reader.GetInt32(0);
                string name = reader.GetString(1);
                remoteMap.Add(id, name);
            }
                
            return remoteMap;
        }
    }
}
