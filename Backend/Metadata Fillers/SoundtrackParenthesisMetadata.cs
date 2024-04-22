using static LocalPlaylistMaster.Backend.Utilities.MetadataFillerExtensions;

namespace LocalPlaylistMaster.Backend.Metadata_Fillers
{
    public class SoundtrackParenthesisMetadata : MetadataBase
    {
        public override string Name => "'soundtrack parenthesis' config";

        public override string ConfigName => "Soundtrack parenthesis";

        public override bool Fill(Track track, out Track modified)
        {
            modified = new Track(track);

            if (IsStandaloneWord("OST", track.Name, out string usedWord) || IsStandaloneWord("O.S.T.", track.Name, out usedWord) || IsStandaloneWord("Soundtrack", track.Name, out usedWord))
            {
                int index = track.Name.IndexOf(usedWord, StringComparison.InvariantCultureIgnoreCase);
                if (index < 0 || index + usedWord.Length >= track.Name.Length || track.Name[index + usedWord.Length] != ')')
                {
                    return false;
                }

                string album = CutLeftToChar(track.Name[..index], '(', out int left);
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

                album = a + "Soundtrack";
                string title = track.Name[..left].Trim(CLEAN_UP_TRIM);
                modified.Album = album;
                modified.Name = title;

                return true;
            }

            return false;
        }
    }
}
