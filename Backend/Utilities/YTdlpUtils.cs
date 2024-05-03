namespace LocalPlaylistMaster.Backend.Utilities
{
    internal static class YTdlpUtils
    {
        public static string GetNameWithoutURLTag(string name)
        {
            return name[..name.LastIndexOf('[')].Trim();
        }

        public static string GetURLTag(string name)
        {
            return name[(name.LastIndexOf('[') + 1)..name.LastIndexOf(']')];
        }

        public static string GetPlaylistId(string url)
        {
            string playlistID;
            const string LIST_URL = "list=";
            if (url.Contains('&'))
            {
                playlistID = url.Split('&').Where(s => s.StartsWith(LIST_URL)).First()[LIST_URL.Length..];
            }
            else
            {
                int listIndex = url.IndexOf(LIST_URL) + LIST_URL.Length;
                playlistID = url[listIndex..];
            }
            return playlistID;
        }
    }
}
