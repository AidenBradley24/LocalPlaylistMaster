namespace LocalPlaylistMaster.Backend
{
    internal class MetadataFiller(FillerSuite suite)
    {
        private readonly FillerSuite suite = suite;

        public List<Track> FillAll(IEnumerable<Track> tracks)
        {
            List<Track> result = [];

            Parallel.ForEach(tracks, (Track track) =>
            {
                track.Backup();
                Track resultTrack = track;
                foreach (MetadataBase filler in suite)
                {
                    try
                    {
                        if (filler.Fill(track, out Track modified))
                        {
                            resultTrack = modified;
                            break;
                        }
                    }
                    catch { }
                }

                lock (result)
                {
                    result.Add(resultTrack);
                }
            });

            return result;
        }

        public static List<Track> ApplyMetadataFillerSuite(Type suiteType, IEnumerable<Track> tracks)
        {
            FillerSuite suite = Activator.CreateInstance(suiteType) as FillerSuite ?? throw new Exception();
            MetadataFiller filler = new(suite);
            return filler.FillAll(tracks);
        }
    }
}
