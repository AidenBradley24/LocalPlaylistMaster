namespace YTAutoMusic
{
    /// <summary>
    /// Creates the append shortcut
    /// </summary>
    internal static class BatchAppendMaker
    {
        public static void Create(DirectoryInfo targetDirectory, string playlistURL)
        {
            string path = Path.Combine(targetDirectory.FullName, "Append Playlist.bat");

            using FileStream fileStream = File.Create(path);
            using StreamWriter writer = new(fileStream);

            writer.WriteLine($"CALL \"{Environment.ProcessPath}\" a \"{targetDirectory.FullName}\" \"{playlistURL}\"");
            writer.WriteLine("PAUSE");

            writer.Close();
        }
    }
}
