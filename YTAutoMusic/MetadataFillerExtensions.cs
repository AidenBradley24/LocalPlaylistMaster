using System;

namespace YTAutoMusic
{
    public static class MetadataFillerExtensions
    {
        /// <summary>
        /// Common seperator characters
        /// </summary>
        public static readonly char[] SEPERATORS = { '-', '\u2012', '\u2013', '\u2014', '\u2015', '|', '·', '\uFF02', '\u0022', '\u201C', '\u201D', '\u201E', '\u201F' };

        /// <summary>
        /// Common quote characters
        /// </summary>
        public static readonly char[] QUOTES = { '\u0022', '\u00AB', '\u00BB', '\u201C', '\u201D', '\u201E', '\uFF02' };

        /// <summary>
        /// Characters used with trim to clean up metadata
        /// </summary>
        public static readonly char[] CLEAN_UP_TRIM = { ' ', '\n', '\t', '\r', '.', ':', '\uFF1A', '-', '\u2012', '\u2013', '\u2014', '\u2015', '|', '·', '\uFF02', '\u0022', '\u201C', '\u201D', '\u201E', '\u201F' };

        /// <summary>
        /// Prepositions that are usually not at the end of a title
        /// </summary>
        public static readonly string[] PREPOSITIONS = { 
            "at", "by", "during", "for", "from", "in", "onto", "of",
            "to", "with", "than", "through", "throughout", "towards"
        };

        /// <summary>
        /// Split a sentence by only the first divider. If there is no split, second half will be "".
        /// </summary>
        /// <param name="sentence">Sentence to split</param>
        /// <returns>(first half, second half)</returns>
        public static (string, string) SplitFirst(string sentence)
        {
            for (int i = 0; i < sentence.Length; i++)
            {
                char c = sentence[i];
                if (SEPERATORS.Contains(c))
                {
                    string first = sentence[..i].Trim();
                    string last = sentence[(i + 1)..].Trim();

                    return (first, last);
                }
            }

            return (sentence.Trim(), "");
        }

        /// <summary>
        /// Create an array with lines seperated out from a description
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        public static string[] LineifyDescription(string description)
        {
            return description.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Check for if word is inside sentence. Insures that word is isolated with only whitespace, an edge, or symbols surounding it.
        /// </summary>
        /// <param name="word">Word to check</param>
        /// <param name="sentence">Sentence to check for word</param>
        /// <param name="usedWord">Returns given work back. (To determine which word was used out of many)</param>
        /// <returns></returns>
        public static bool IsStandaloneWord(string word, string sentence, out string usedWord)
        {
            int index = sentence.IndexOf(word, StringComparison.InvariantCultureIgnoreCase);
            usedWord = word;

            if (index == -1)
            {
                return false;
            }

            if (index != 0 && char.IsLetterOrDigit(sentence[index - 1]))
            {
                return false;
            }

            if (index + word.Length < sentence.Length - 1 && char.IsLetterOrDigit(sentence[index + word.Length]))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Calls the 'contains' method on sentence with InvariantCultureIgnoreCase. In same format as IsStandaloneWord.
        /// </summary>
        /// <param name="word">Word to check</param>
        /// <param name="sentence">Sentence to check for word</param>
        /// <param name="usedWord">Returns given work back. (To determine which word was used out of many)</param>
        /// <returns></returns>
        public static bool IsWord(string word, string sentence, out string usedWord)
        {
            usedWord = word;

            if (sentence.Contains(word, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Calls the 'starts with' method on sentence with InvariantCultureIgnoreCase. In same format as IsStandaloneWord.
        /// </summary>
        /// <param name="word">Word to check</param>
        /// <param name="sentence">Sentence to check for word</param>
        /// <param name="usedWord">Returns given work back. (To determine which word was used out of many)</param>
        /// <returns></returns>
        public static bool StartsWithStandaloneWord(string word, string sentence, out string usedWord)
        {
            usedWord = word;

            if(!sentence.StartsWith(word, StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            if (word.Length < sentence.Length - 1 && char.IsLetterOrDigit(sentence[word.Length]))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Return a piece of a string starting at the specified character
        /// </summary>
        /// <param name="sentence">String to split</param>
        /// <param name="target">Character to start at</param>
        /// <param name="hit">Index of hit character</param>
        /// <returns></returns>
        public static string CutLeftToChar(string sentence, char target, out int hit)
        {
            sentence = sentence.Trim();

            for (int i = sentence.Length - 1; i >= 0; i--)
            {
                if (sentence[i] == target)
                {
                    hit = i;
                    return sentence[(i + 1)..];
                }
            }

            hit = -1;
            return sentence;
        }

        /// <summary>
        /// Is a string a container for a number?
        /// </summary>
        /// <param name="section">String to check</param>
        /// <returns>True if number body</returns>
        public static bool IsNumberBody(string section)
        {
            section = section.Trim('(', ')');
            section = section.TrimStart('#');
            section = section.Trim(CLEAN_UP_TRIM);

            return IsNumber(section);
        }

        /// <summary>
        /// Checks to see if a string is entirely digits
        /// </summary>
        /// <param name="section"></param>
        /// <returns></returns>
        public static bool IsNumber(string section)
        {
            return section.All(char.IsDigit);
        }

        public static bool Contains(string[] span, string word)
        {
            foreach (string s in span)
            {
                if(s.Equals(word, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
