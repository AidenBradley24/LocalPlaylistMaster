using YTAutoMusic.Metadata_Fillers;

namespace YTAutoMusicTests.Metadata_Filler_Tests
{
    /// <summary>
    /// Test <see cref="SoundtrackMetadata"/>
    /// </summary>
    public class SoundtrackTests
    {
        [Fact]
        public void Soundtrack_Name_1()
        {
            var tagFile = TagFileCreater.CreateTemp();
            var data = new SoundtrackMetadata();

            string title = "Random OST - Song Name";
            string description = "nonsense";

            Assert.True(data.Fill(tagFile, title, description));

            Assert.Equal("Song Name", tagFile.Tag.Title);
            Assert.Equal("Random Soundtrack", tagFile.Tag.Album);
        }

        [Fact]
        public void Soundtrack_Name_2()
        {
            var tagFile = TagFileCreater.CreateTemp();
            var data = new SoundtrackMetadata();

            string title = "Random O.S.T. - Song Name";
            string description = "nonsense";

            Assert.True(data.Fill(tagFile, title, description));

            Assert.Equal("Song Name", tagFile.Tag.Title);
            Assert.Equal("Random Soundtrack", tagFile.Tag.Album);
        }

        [Fact]
        public void Soundtrack_Name_3()
        {
            var tagFile = TagFileCreater.CreateTemp();
            var data = new SoundtrackMetadata();

            string title = "Random Soundtrack - Song Name";
            string description = "nonsense";

            Assert.True(data.Fill(tagFile, title, description));

            Assert.Equal("Song Name", tagFile.Tag.Title);
            Assert.Equal("Random Soundtrack", tagFile.Tag.Album);
        }

        [Fact]
        public void Name_Soundtrack_1()
        {
            var tagFile = TagFileCreater.CreateTemp();
            var data = new SoundtrackMetadata();

            string title = "Song Name - Random OST";
            string description = "nonsense";

            Assert.True(data.Fill(tagFile, title, description));

            Assert.Equal("Song Name", tagFile.Tag.Title);
            Assert.Equal("Random Soundtrack", tagFile.Tag.Album);
        }

        [Fact]
        public void Name_Soundtrack_2()
        {
            var tagFile = TagFileCreater.CreateTemp();
            var data = new SoundtrackMetadata();

            string title = "Song Name - Random O.S.T.";
            string description = "nonsense";

            Assert.True(data.Fill(tagFile, title, description));

            Assert.Equal("Song Name", tagFile.Tag.Title);
            Assert.Equal("Random Soundtrack", tagFile.Tag.Album);
        }

        [Fact]
        public void Name_Soundtrack_3()
        {
            var tagFile = TagFileCreater.CreateTemp();
            var data = new SoundtrackMetadata();

            string title = "Song Name - Random Soundtrack";
            string description = "nonsense";

            Assert.True(data.Fill(tagFile, title, description));

            Assert.Equal("Song Name", tagFile.Tag.Title);
            Assert.Equal("Random Soundtrack", tagFile.Tag.Album);
        }

        [Fact]
        public void IndexRemoval_1()
        {
            var tagFile = TagFileCreater.CreateTemp();
            var data = new SoundtrackMetadata();

            string title = "Random Soundtrack - 1 - Song Name";
            string description = "nonsense";

            Assert.True(data.Fill(tagFile, title, description));

            Assert.Equal("Song Name", tagFile.Tag.Title);
            Assert.Equal("Random Soundtrack", tagFile.Tag.Album);
        }

        [Fact] public void IndexRemoval_2()
        {
            var tagFile = TagFileCreater.CreateTemp();
            var data = new SoundtrackMetadata();

            string title = "Random Ost: 1 - Song Name";
            string description = "nonsense";

            Assert.True(data.Fill(tagFile, title, description));

            Assert.Equal("Song Name", tagFile.Tag.Title);
            Assert.Equal("Random Soundtrack", tagFile.Tag.Album);
        }

        [Fact]
        public void CompleteMess_1()
        {
            var tagFile = TagFileCreater.CreateTemp();
            var data = new SoundtrackMetadata();

            string title = "\"Random O.S.T. \" ( 3278 ) \"Song Name \"";
            string description = "nonsense";

            Assert.True(data.Fill(tagFile, title, description));

            Assert.Equal("Song Name", tagFile.Tag.Title);
            Assert.Equal("Random Soundtrack", tagFile.Tag.Album);
        }

        [Fact]
        public void ComplexIndex()
        {
            var tagFile = TagFileCreater.CreateTemp();
            var data = new SoundtrackMetadata();

            string title = "Random OST - Song Name (1-2)";
            string description = "nonsense";

            Assert.True(data.Fill(tagFile, title, description));

            Assert.Equal("Song Name", tagFile.Tag.Title);
            Assert.Equal("Random Soundtrack", tagFile.Tag.Album);
        }

        [Fact]
        public void NoNameTrack_1()
        {
            var tagFile = TagFileCreater.CreateTemp();
            var data = new SoundtrackMetadata();

            string title = "Track #1 - Random OST";
            string description = "nonsense";

            Assert.True(data.Fill(tagFile, title, description));

            Assert.Equal("Track #1 - Random Soundtrack", tagFile.Tag.Title);
            Assert.Equal("Random Soundtrack", tagFile.Tag.Album);
        }

        [Fact]
        public void NoNameTrack_2()
        {
            var tagFile = TagFileCreater.CreateTemp();
            var data = new SoundtrackMetadata();

            string title = "Random OST - Track #1";
            string description = "nonsense";

            Assert.True(data.Fill(tagFile, title, description));

            Assert.Equal("Track #1 - Random Soundtrack", tagFile.Tag.Title);
            Assert.Equal("Random Soundtrack", tagFile.Tag.Album);
        }

        [Fact]
        public void PrepositionWithNumber_1()
        {
            var tagFile = TagFileCreater.CreateTemp();
            var data = new SoundtrackMetadata();

            string title = "Up to 4 - Random OST";
            string description = "nonsense";

            Assert.True(data.Fill(tagFile, title, description));

            Assert.Equal("Up to 4", tagFile.Tag.Title);
            Assert.Equal("Random Soundtrack", tagFile.Tag.Album);
        }

        [Fact]
        public void NumberInMiddle()
        {
            var tagFile = TagFileCreater.CreateTemp();
            var data = new SoundtrackMetadata();

            string title = "Walking 4 Dogs - Random OST";
            string description = "nonsense";

            Assert.True(data.Fill(tagFile, title, description));

            Assert.Equal("Walking 4 Dogs", tagFile.Tag.Title);
            Assert.Equal("Random Soundtrack", tagFile.Tag.Album);
        }
    }
}
