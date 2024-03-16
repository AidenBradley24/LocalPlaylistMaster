using System.Xml.Linq;

namespace LocalPlaylistMaster.Backend.Playlist_Files
{
    internal class XspfPlain : XspfFile
    {
        public override string FileName => "playlist.xspf";

        public override string Prefix => null;

        public override string NsURL => null;

        public override string ConfigName => "plain xspf playlist";

        public override string AppPlaylistURL => null;

        public override string AppTrackURL => null;

        public override XElement GetPlaylistExtension(XNamespace appNS)
        {
            return null;
        }

        public override XElement GetPlaylistItemExtension(XNamespace appNS, int index)
        {
            return null;
        }
    }
}
