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
        public abstract void Build(DirectoryInfo targetDirectory, DirectoryInfo trackDirectory, Playlist bundle);
    }
}