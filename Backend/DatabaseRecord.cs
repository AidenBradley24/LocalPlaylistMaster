using System.Xml.Serialization;

namespace LocalPlaylistMaster.Backend
{
    [XmlRoot("PlaylistRecord")]
    public record DatabaseRecord
    {
        public string? name;
    }
}
