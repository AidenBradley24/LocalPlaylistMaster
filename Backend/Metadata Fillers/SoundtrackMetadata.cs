using static LocalPlaylistMaster.Backend.Utilities.MetadataFillerExtensions;

namespace LocalPlaylistMaster.Backend.Metadata_Fillers
{
    public class SoundtrackMetadata : MetadataBase
    {
        public override string Name => "'soundtrack' config";
        public override string ConfigName => "Soundtrack";

        public override bool Fill(Track track, out Track modified)
        {
            modified = new Track(track);

            if (IsStandaloneWord("OST", track.Name, out string usedWord) || IsStandaloneWord("O.S.T", track.Name, out usedWord) || IsStandaloneWord("Soundtrack", track.Name, out usedWord))
            {
                var bits = track.Name.Split(SEPERATORS, StringSplitOptions.RemoveEmptyEntries);

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

                    modified.Album = a + "Soundtrack";

                    i++;

                    string? t = null;

                    for (; i < bits.Length; i++)
                    {
                        if (i == blacklist) continue;

                        string bit = ProcessTitle(bits[i], modified.Album);

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

                            string bit = ProcessTitle(bits[i], modified.Album);

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
                    modified.Name = t;
                }

                return true;
            }

            return false;
        }

        private static string ProcessTitle(string bit, string album)
        {
            bit = bit.Trim();
            var words = bit.Split(' ', '\t', '\r');

            if (StartsWithStandaloneWord("track", bit, out _) || 
                StartsWithStandaloneWord("song", bit, out _) || 
                StartsWithStandaloneWord("part", bit, out _))
            {

                if (words.Length > 1 && IsNumberBody(words[1]))
                {
                    return $"{bit} - {album}";
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
