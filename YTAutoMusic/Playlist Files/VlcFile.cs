using System.Xml.Linq;

namespace YTAutoMusic.Playlist_Files
{
    internal class VlcFile : XspfFile
    {
        public override string FileName => "VLC Playlist.xspf";

        public override string Prefix => "vlc";

        public override string NsURL => "http://www.videolan.org/vlc/playlist/ns/0/";

        public override string ConfigName => "VLC playlist";

        public override string AppPlaylistURL => "http://www.videolan.org/vlc/playlist/0";

        public override string AppTrackURL => "http://www.videolan.org/vlc/playlist/0";

        public override XElement GetPlaylistExtension(XNamespace appNS)
        {
            return new XElement(appNS + "item",
                new XAttribute("tid", 0)
            );
        }

        public override XElement GetPlaylistItemExtension(XNamespace appNS, int index)
        {
            return new XElement(appNS + "id", index);
        }
    }
}
