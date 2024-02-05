using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LocalPlaylistMaster.Backend
{
    [XmlRoot("PlaylistSet")]
    public record PlaylistSet
    {
        public PlaylistSet()
        {
            remotes = new();
        }

        public string name;
        public List<Remote> remotes;
        // add tracks
    }
}
