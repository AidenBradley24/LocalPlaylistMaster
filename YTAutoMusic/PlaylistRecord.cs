using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LocalPlaylistMaster.Backend
{
    [XmlRoot("PlaylistRecord")]
    public record PlaylistRecord
    {
        public string name;
    }
}
