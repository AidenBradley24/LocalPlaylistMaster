namespace YTAutoMusic
{
    /// <summary>
    /// A meta data filling scheme; Only one can be used per song.
    /// </summary>
    public abstract class MetadataBase
    {
        /// <summary>
        /// Fill tag file metadata based on an abstract scheme
        /// </summary>
        /// <param name="tagFile">Tag file to modify</param>
        /// <param name="title">YT video title</param>
        /// <param name="description">YT video description</param>
        /// <returns>True if successful</returns>
        /// <exception cref="Exception">On a failure after the attempt has begun</exception>
        public abstract bool Fill(TagLib.File tagFile, string title, string description);

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
