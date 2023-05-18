using YTAutoMusic;

namespace YTAutoMusicTests.Metadata_Filler_Tests
{
    /// <summary>
    /// Tests <see cref="MetadataFillerExtensions"/>
    /// </summary>
    public class MetadataFillerExtensionsTests
    {
        [Fact]
        public void IsStandaloneWordTest_1()
        {
            Assert.True(MetadataFillerExtensions.IsStandaloneWord("OST", "Random OST - Song Name", out _));
        }

        [Fact]
        public void IsStandaloneWordTest_2()
        {
            Assert.False(MetadataFillerExtensions.IsStandaloneWord("and", "Random OST - Song Name", out _));
        }

        [Fact]
        public void IsStandaloneWordTest_3()
        {
            Assert.True(MetadataFillerExtensions.IsStandaloneWord("OST", "Random OST-Song Name", out _));
        }
    }
}
