using static LocalPlaylistMaster.Backend.Utilities.MetadataFillerExtensions;

namespace LocalPlaylistMaster.Backend.Metadata_Fillers
{
    public class MusicKeywordMetadata : MetadataBase
    {
        public override string Name => "'music keyword' config";
        public override string ConfigName => "Music keyword";

        public override bool Fill(Track track, out Track modified)
        {
            modified = new Track(track);

            if (IsWord("Music ", track.Description, out string usedWord) || IsWord("Title ", track.Description, out usedWord))
            {
                var lines = LineifyDescription(track.Description).AsEnumerable();

                var possibleLines = lines.Where(l => l.StartsWith(usedWord, StringComparison.InvariantCultureIgnoreCase));
                if (!possibleLines.Any())
                {
                    throw new FormatException("Not real 'music keyword'");
                }

                string titleLine = possibleLines.First();
                titleLine = titleLine[usedWord.Length..];

                (string t, string a) = SplitFirst(titleLine);

                t = t.Trim(CLEAN_UP_TRIM);
                a = a.Trim(CLEAN_UP_TRIM);

                if (!track.Name.Contains(t))
                {
                    throw new FormatException("Not real 'music keyword'");
                }

                modified.Name = t;
                modified.Album = string.IsNullOrWhiteSpace(a) ? "" : a;
                return true;
            }

            return false;
        }
    }
}
