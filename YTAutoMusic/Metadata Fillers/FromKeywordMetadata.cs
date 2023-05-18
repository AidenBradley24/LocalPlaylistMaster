using static YTAutoMusic.MetadataFillerExtensions;

namespace YTAutoMusic.Metadata_Fillers
{
    public class FromKeywordMetadata : MetadataBase
    {
        public override string Name => "'from keyword' config";

        public override string ConfigName => "From keyword";

        public override bool Fill(TagLib.File tagFile, string title, string description)
        {
            if (IsStandaloneWord("From", title, out string usedWord))
            {
                if (!title.Contains($"({usedWord}", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new FormatException("Not real 'from keyword' config");
                }

                string[] bits = title.Split("(", StringSplitOptions.TrimEntries);

                string t = bits[0].Trim();
                string a = bits[1][usedWord.Length..^1];

                foreach (char q in QUOTES)
                {
                    a = a.Replace(q.ToString(), "");
                }

                a = a.Trim();

                tagFile.Tag.Title = t;
                tagFile.Tag.Album = a;

                return true;
            }

            return false;
        }
    }
}
