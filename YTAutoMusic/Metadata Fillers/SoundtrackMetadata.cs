using static YTAutoMusic.MetadataFillerExtensions;

namespace YTAutoMusic.Metadata_Fillers
{
    public class SoundtrackMetadata : MetadataBase
    {
        public override string Name => "'soundtrack' config";

        public override string ConfigName => "Soundtrack";

        private TagLib.File tagFile;

        public override bool Fill(TagLib.File tagFile, string title, string description)
        {
            if (IsStandaloneWord("OST", title, out string usedWord) || IsStandaloneWord("O.S.T", title, out usedWord) || IsStandaloneWord("Soundtrack", title, out usedWord))
            {
                this.tagFile = tagFile;
                var bits = title.Split(SEPERATORS, StringSplitOptions.RemoveEmptyEntries);

                int i;
                for (i = 0; i < bits.Length; i++)
                {
                    if (IsStandaloneWord(usedWord, bits[i], out _))
                    {
                        break;
                    }
                }

                int soundtrackIndex = i;

                if (i < bits.Length)
                {
                    string album = bits[i].Trim().Trim(CLEAN_UP_TRIM);
                    int blacklist = -1;

                    if (album == usedWord)
                    {
                        i--;
                        if (i < 0)
                        {
                            throw new IndexOutOfRangeException("Can't find soundtrack name");
                        }

                        blacklist = i;

                        album = bits[i].Trim();
                        if (album.Length == 0)
                        {
                            throw new FormatException("Can't find soundtrack name");
                        }
                    }

                    if (usedWord.Equals("O.S.T", StringComparison.InvariantCultureIgnoreCase))
                    {
                        album = album.Replace("O.S.T", "OST", StringComparison.InvariantCultureIgnoreCase);
                    }

                    string a = "";

                    foreach (string word in album.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
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

                    i++;

                    string t = null;

                    for (; i < bits.Length; i++)
                    {
                        if (i == blacklist) continue;

                        string bit = ProcessTitle(bits[i]);

                        if (string.IsNullOrWhiteSpace(bit) || IsNumberBody(bit))
                        {
                            continue;
                        }

                        t = bit;
                        break;
                    }

                    i = soundtrackIndex - 1;

                    if (t == null)
                    {
                        for (; i >= 0; i--)
                        {
                            if (i == blacklist) continue;

                            string bit = ProcessTitle(bits[i]);

                            if (string.IsNullOrWhiteSpace(bit) || IsNumberBody(bit))
                            {
                                continue;
                            }

                            t = bit;
                            break;
                        }
                    }

                    if (t == null)
                    {
                        throw new FormatException("Title failed");
                    }

                    t = t.Trim(CLEAN_UP_TRIM);

                    tagFile.Tag.Title = t;
                }

                tagFile.Tag.Genres = new string[] { "Soundtrack" };

                return true;
            }

            return false;
        }

        private string ProcessTitle(string bit)
        {
            bit = bit.Trim();
            var words = bit.Split(' ', '\t', '\r');

            if (StartsWithStandaloneWord("track", bit, out _) || 
                StartsWithStandaloneWord("song", bit, out _) || 
                StartsWithStandaloneWord("part", bit, out _))
            {

                if (words.Length > 1 && IsNumberBody(words[1]))
                {
                    return $"{bit} - {tagFile.Tag.Album}";
                }
            }

            bit = bit.TrimStart('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');

            if (words.Length >= 2 && IsNumberBody(words[^1]) && !Contains(PREPOSITIONS, words[^2]))
            {
                bit = bit.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
            }

            bit = bit.TrimEnd('(');
            bit = bit.TrimStart(')');


            return bit;
        }
    }
}
