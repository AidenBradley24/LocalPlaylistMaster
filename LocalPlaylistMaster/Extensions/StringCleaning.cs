using System.Text.RegularExpressions;

namespace LocalPlaylistMaster.Extensions
{
    public static partial class StringCleaning
    {
        public static string CleanElementName(string name)
        {
            name = name.Trim().Replace(' ', '_');
            name = AlphaNum().Replace(name, "");
            if (!char.IsLetter(name[0]) && name[0] != '_')
            {
                name = "_" + name;
            }
            return name;
        }

        [GeneratedRegex("[^a-zA-Z0-9_]")]
        private static partial Regex AlphaNum();
    }
}
