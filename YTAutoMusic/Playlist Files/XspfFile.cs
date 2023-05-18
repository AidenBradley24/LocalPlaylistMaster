using System.Text;
using System.Text.Encodings.Web;
using System.Xml.Linq;
using TagLib.Id3v2;

namespace YTAutoMusic.Playlist_Files
{
    /// <summary>
    /// Base class for XSPF playlists derivatives
    /// </summary>
    internal abstract class XspfFile : PlaylistFile
    {
        protected PlaylistBundle bundle;

        /// <summary>
        /// Output file name such as 'playlist.xspf'
        /// </summary>
        public abstract string FileName { get; }

        /// <summary>
        /// app xml namespace prefix
        /// </summary>
        public abstract string Prefix { get; }

        /// <summary>
        /// url of app xml namespace definition
        /// </summary>
        public abstract string NsURL { get; }

        /// <summary>
        /// url of app playlist definition
        /// </summary>
        public abstract string AppPlaylistURL { get; }

        /// <summary>
        /// url of app track definition
        /// </summary>
        public abstract string AppTrackURL { get; }

        public abstract XElement GetPlaylistExtension(XNamespace appNS);

        public abstract XElement GetPlaylistItemExtension(XNamespace appNS, int index);

        public override void Build(DirectoryInfo targetDirectory, DirectoryInfo trackDirectory, PlaylistBundle bundle)
        {
            this.bundle = bundle;

            string header = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><playlist version=\"1\" xmlns=\"http://xspf.org/ns/0/\"";

            bool hasApp = !string.IsNullOrWhiteSpace(Prefix);

            if (hasApp)
            {
                header += $" xmlns:{Prefix}=\"{NsURL}\"";
            }
            
            header += "></playlist>";

            XDocument doc = XDocument.Parse(header);
            XElement playlist = doc.FirstNode as XElement;
            XNamespace ns = playlist.GetDefaultNamespace();
            XNamespace appNS = hasApp ? playlist.GetNamespaceOfPrefix(Prefix) : null;
            XElement tracklist = new(ns + "trackList");

            playlist.Add(
                new XElement(ns + "title", bundle.Name),
                new XElement(ns + "info", $"https://www.youtube.com/playlist?list={bundle.ID}"),
                tracklist
            );

            if (hasApp)
            {
                XElement playlistExtension = new(ns + "extension", new object[]
                {
                    new XAttribute("application", AppPlaylistURL),
                    GetPlaylistExtension(appNS),
                });

                playlist.Add(playlistExtension);
            }

            int counter = 0;
            var url = UrlEncoder.Default;

            foreach (FileInfo file in trackDirectory.EnumerateFiles())
            {
                string location = "file:///tracks/" + url.Encode($"{file.Name}");

                TagLib.File tagFile = TagLib.File.Create(file.FullName);

                Tag idTag = (Tag)tagFile.GetTag(TagLib.TagTypes.Id3v2); // for reading the youtube id
                PrivateFrame p = PrivateFrame.Get(idTag, "yt-id", false);

                string info;
                if (p != null)
                {
                    string id = Encoding.Unicode.GetString(p.PrivateData.Data);
                    info = $"https://www.youtube.com/watch?v={id}&list={bundle.ID}";
                }
                else
                {
                    info = $"Added to playlist without YouTube";
                }

                XElement trackElement = new (ns + "track");
                trackElement.Add(new XElement(ns + "location", location), 
                    new XElement(ns + "title", tagFile.Tag.Title), 
                    new XElement(ns + "info", info));

                if (hasApp)
                {
                    XElement extension = new(ns + "extension");

                    extension.Add(new object[]
                    {
                        new XAttribute("application", AppTrackURL),
                        GetPlaylistItemExtension(appNS, counter)
                    });

                    trackElement.Add(extension);
                }

                tracklist.Add(trackElement);
                counter++;
            }

            using FileStream stream = new(Path.Combine(targetDirectory.FullName, FileName), FileMode.Create);
            doc.Save(stream);
        }
    }
}
