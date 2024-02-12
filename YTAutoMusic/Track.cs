
namespace LocalPlaylistMaster.Backend
{
    public record Track(int Id, string Name, int Remote, string RemoteId,
        string Artists, string Album, string Description, int Rating,
        int TimeInSeconds, TrackSettings Settings)
    {
        public string[] GetArtists()
        {
            return Artists.Split(',');
        }

        public const int UNINITIALIZED = -1;
    }

    [Flags]
    public enum TrackSettings
    {
        none = 0,
        removeMe = 1 << 0,
        locked = 1 << 1,
    }
}
