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
        private readonly SQLiteConnection db;

        public PlaylistManager(string folderPath)
        {
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
        }

        ~PlaylistManager()
        {
            db?.Close();
        }
    }
}
