namespace LocalPlaylistMaster.Backend.Metadata_Fillers
{
    /// <summary>
    /// Metadata fillers for YouTube
    /// </summary>
    public class YTSuite : FillerSuite
    {
        protected override Type[] Fillers => new Type[]
        {
            typeof(ProvidedMetadata),
            typeof(SoundtrackParenthesisMetadata),
            typeof(SoundtrackMetadata),
            typeof(FromKeywordMetadata),
            typeof(MusicKeywordMetadata),
        };
    }
}
