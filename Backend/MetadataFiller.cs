using System.Diagnostics;

namespace LocalPlaylistMaster.Backend
{
    internal class MetadataFiller(FillerSuite suite)
    {
        private readonly FillerSuite suite = suite;

        public List<Track> FillAll(IEnumerable<Track> tracks)
        {
            List<Track> result = [];
            foreach (var track in tracks)
            {
                bool filled = false;
                foreach (MetadataBase filler in suite)
                {
                    try
                    {
                        if (filler.Fill(track, out Track modified))
                        {
                            Trace.WriteLine($"Filled metadata with {filler.Name}");
                            result.Add(modified);
                            filled = true;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"{filler.Name} failed.\n{ex.Message}");
                    }
                }

                if (!filled) result.Add(track);
            }

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
