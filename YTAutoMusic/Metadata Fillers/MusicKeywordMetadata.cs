using static YTAutoMusic.MetadataFillerExtensions;

namespace YTAutoMusic.Metadata_Fillers
{
    public class MusicKeywordMetadata : MetadataBase
    {
        public override string Name => "'music keyword' config";

        public override string ConfigName => "Music keyword";

        public override bool Fill(TagLib.File tagFile, string title, string description)
        {
            if (IsWord("Music ", description, out string usedWord) || IsWord("Title ", description, out usedWord))
            {
                var lines = LineifyDescription(description).AsEnumerable();

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

                if (!title.Contains(t))
                {
                    throw new FormatException("Not real 'music keyword'");
                }

                if (string.IsNullOrWhiteSpace(a))
                {
                    tagFile.Tag.Title = t;
                    tagFile.Tag.Album = "";
                }
                else
                {
                    tagFile.Tag.Title = t;
                    tagFile.Tag.Album = a;
                }

                return true;
            }

            return false;
        }
    }
}
