using static LocalPlaylistMaster.Backend.Utilities.MetadataFillerExtensions;

namespace LocalPlaylistMaster.Backend.Metadata_Fillers
{
    public class FromKeywordMetadata : MetadataBase
    {
        public override string Name => "'from keyword' config";
        public override string ConfigName => "From keyword";

        public override bool Fill(Track track, out Track modified)
        {
            modified = new(track);

            if (IsStandaloneWord("From", track.Name, out string usedWord))
            {
                if (!track.Name.Contains($"({usedWord}", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new FormatException("Not real 'from keyword' config");
                }

                string[] bits = track.Name.Split("(", StringSplitOptions.TrimEntries);
                string t = bits[0].Trim();
                string a = bits[1][usedWord.Length..^1];

                foreach (char q in QUOTES)
                {
                    a = a.Replace(q.ToString(), "");
                }

                a = a.Trim();
                modified.Name = t;
                modified.Album = a;
                return true;
            }

            return false;
        }
    }
}
