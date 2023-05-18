using static YTAutoMusic.MetadataFillerExtensions;

namespace YTAutoMusic.Metadata_Fillers
{
    public class SoundtrackParenthesisMetadata : MetadataBase
    {
        public override string Name => "'soundtrack parenthesis' config";

        public override string ConfigName => "Soundtrack parenthesis";

        public override bool Fill(TagLib.File tagFile, string title, string description)
        {
            if (IsStandaloneWord("OST", title, out string usedWord) || IsStandaloneWord("O.S.T.", title, out usedWord) || IsStandaloneWord("Soundtrack", title, out usedWord))
            {
                int index = title.IndexOf(usedWord, StringComparison.InvariantCultureIgnoreCase);
                if (index < 0 || index + usedWord.Length >= title.Length || title[index + usedWord.Length] != ')')
                {
                    return false;
                }

                string album = CutLeftToChar(title[..index], '(', out int left);
                if (usedWord == "O.S.T.")
                {
                    album = album.Replace("O.S.T.", "OST");
                }

                string a = "";

                foreach (string word in album.Split(' '))
                {
                    string trimmedWord = word.Trim(CLEAN_UP_TRIM);

                    if (trimmedWord.Equals("OST", StringComparison.InvariantCultureIgnoreCase) ||
                        trimmedWord.Equals("Soundtrack", StringComparison.InvariantCultureIgnoreCase))
                    {
                        break;
                    }

                    a += word + " ";
                }

                tagFile.Tag.Album = a + "Soundtrack";

                string t = title[..left].Trim(CLEAN_UP_TRIM);
                tagFile.Tag.Title = t;

                tagFile.Tag.Genres = new string[] { "Soundtrack" };

                return true;
            }

            return false;
        }
    }
}
