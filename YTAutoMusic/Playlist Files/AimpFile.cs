using System.Xml.Linq;

namespace YTAutoMusic.Playlist_Files
{
    internal class AimpFile : XspfFile
    {
        public override string FileName => "AIMP playlist.xspf";

        public override string Prefix => "aimp";

        public override string NsURL => "http://www.aimp.ru/playlist/ns/0/";

        public override string AppPlaylistURL => "http://www.aimp.ru/playlist/summary/0";

        public override string AppTrackURL => "http://www.aimp.ru/playlist/track/0";

        public override string ConfigName => "AIMP playlist";

        public override XElement GetPlaylistExtension(XNamespace appNS)
        {
            return new XElement(appNS + "prop",
               new XAttribute("name", "Name"),
               bundle.Name
            );
        }

        public override XElement GetPlaylistItemExtension(XNamespace appNS, int index)
        {
            return new XElement(appNS + "queueIndex", index);
        }
    }
}
