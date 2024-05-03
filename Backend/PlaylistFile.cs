namespace LocalPlaylistMaster.Backend
{
    /// <summary>
    /// A generator for a playlist file
    /// </summary>
    internal abstract class PlaylistFile
    {
        /// <summary>
        /// Create the playlist file.
        /// </summary>
        /// <param name="targetDirectory">Directory to place the file</param>
        /// <param name="trackDirectory">Location of the tagged audio files</param>
        /// <param name="bundle">Playlist information</param>
        /// <param name="trackFileMap">Mapping audio file to track record</param>
        public abstract void Build(DirectoryInfo targetDirectory, Playlist bundle, Dictionary<FileInfo, Track> trackFileMap, bool fullPath);
    }
}