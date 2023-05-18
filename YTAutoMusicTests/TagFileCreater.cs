namespace YTAutoMusicTests
{
    internal static class TagFileCreater
    {
        public static TagLib.File CreateTemp()
        {
            var directory = new FileInfo(Environment.ProcessPath).Directory;
            return TagLib.File.Create(Path.Combine(directory.FullName, "Resources", "Test.mp3"));
        }
    }
}
