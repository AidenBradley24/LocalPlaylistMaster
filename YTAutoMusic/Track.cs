namespace LocalPlaylistMaster.Backend
{
    public record Track(int Id, string Name, int Remote, string RemoteId,
        string Artists, string Album, string Description, int Rating,
        int TimeInSeconds, bool RemoveMe)
    {
        public string[] GetArtists()
        {
            return Artists.Split(',');
        }
    }
}
