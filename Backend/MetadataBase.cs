namespace LocalPlaylistMaster.Backend
{
    /// <summary>
    /// A meta data filling scheme; Only one can be used per track.
    /// </summary>
    public abstract class MetadataBase
    {
        /// <summary>
        /// Fill track metadata based on an abstract scheme.
        /// </summary>
        /// <param name="track">Source track</param>
        /// <param name="modifiedTrack">Modified track</param>
        /// <exception cref="Exception">If failed</exception>
        /// <returns>True if modified and successful</returns>
        public abstract bool Fill(Track track, out Track modifiedTrack);

        /// <summary>
        /// Shown name of filler
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Name inside of config file "'NAME' filler"
        /// </summary>
        public abstract string ConfigName { get; }
    }
}
