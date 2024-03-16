using System.Reflection;
using System.Configuration;
using System.Globalization;
using System.Formats.Tar;
using System.Diagnostics;

namespace LocalPlaylistMaster.Backend
{
    internal class MetadataFiller
    {
        private readonly FillerSuite suite;

        public MetadataFiller(FillerSuite suite)
        {
            this.suite = suite;
        }

        public List<Track> FillAll(IEnumerable<Track> tracks)
        {
            List<Track> result = new();
            foreach (var track in tracks)
            {
                foreach (MetadataBase filler in suite)
                {
                    try
                    {
                        if (filler.Fill(track, out Track modified))
                        {
                            Trace.WriteLine($"Filled metadata with {filler.Name}");
                            result.Add(modified);
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"{filler.Name} failed.\n{ex.Message}");
                    }
                }
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
