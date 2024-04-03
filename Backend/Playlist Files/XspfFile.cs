using System.Text.Encodings.Web;
using System.Xml.Linq;

namespace LocalPlaylistMaster.Backend.Playlist_Files
{
    /// <summary>
    /// Base class for XSPF playlists derivatives
    /// </summary>
    internal class XspfFile : PlaylistFile
    {
        public override void Build(DirectoryInfo targetDirectory, Playlist bundle, Dictionary<FileInfo, Track> trackFileMap, bool fullPath)
        {
            string header = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><playlist version=\"1\" xmlns=\"http://xspf.org/ns/0/\"></playlist>";
            XDocument doc = XDocument.Parse(header);
            XElement playlist = doc.FirstNode as XElement ?? throw new Exception();
            XNamespace ns = playlist.GetDefaultNamespace();
            XElement tracklist = new(ns + "trackList");

            playlist.Add(
                new XElement(ns + "title", bundle.Name),
                new XElement(ns + "info", bundle.Description),
                tracklist
            );

            var url = UrlEncoder.Default;

            foreach (var pair in trackFileMap)
            {
                FileInfo file = pair.Key;
                Track track = pair.Value;

                string location = fullPath ? "file:///" + url.Encode(file.FullName) : "file:///tracks/" + url.Encode(file.Name);
                string title = track.Name;
                string creator = track.Artists;
                string album = track.Album;
                int duration = TimeSpan.FromSeconds(track.TimeInSeconds).Milliseconds;

                XElement trackElement = new(ns + "track");
                trackElement.Add(new XElement(ns + "location", location),
                    new XElement(ns + "title", title),
                    new XElement(ns + "creator", creator),
                    new XElement(ns + "album", album),
                    new XElement(ns + "duration", duration));

                tracklist.Add(trackElement);
            }

            using FileStream stream = new(Path.Combine(targetDirectory.FullName, "playlist.xspf"), FileMode.Create);
            doc.Save(stream);
        }
    }
}
