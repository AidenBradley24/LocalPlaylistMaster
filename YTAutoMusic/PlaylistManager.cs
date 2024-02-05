using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LocalPlaylistMaster.Backend
{
    /// <summary>
    /// Manages all things playlist
    /// </summary>
    public class PlaylistManager
    {
        public PlaylistSet playlist;

        public PlaylistManager()
        {

        }

        public PlaylistManager(string folderPath)
        {
            XmlSerializer serializer = new(typeof(PlaylistSet));
            string hostFile = Path.Join(folderPath, "host");
            using FileStream stream = new(hostFile, FileMode.OpenOrCreate);
            playlist = (PlaylistSet)serializer.Deserialize(stream);
        }
    }
}
