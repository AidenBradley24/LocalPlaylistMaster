using LocalPlaylistMaster.Backend.Metadata_Fillers;
using LocalPlaylistMaster.Backend;
using Xunit;

namespace BackendTest.Metadata_Filler_Tests
{
    /// <summary>
    /// Test <see cref="SoundtrackMetadata"/>
    /// </summary>
    public class SoundtrackTests
    {
        [Fact]
        public void Soundtrack_Name_1()
        {
            var data = new SoundtrackMetadata();

            const string NAME = "Random OST - Song Name";
            const string DESCRIPTION = "nonsense";

            Track track = new()
            {
                Name = NAME,
                Description = DESCRIPTION
            };

            Assert.True(data.Fill(track, out Track modified));

            // should not modify original
            Assert.Equal(NAME, track.Name);
            Assert.Equal(DESCRIPTION, track.Description);

            Assert.Equal("Song Name", modified.Name);
            Assert.Equal("Random Soundtrack", modified.Album);
        }

        [Fact]
        public void Soundtrack_Name_2()
        {
            var data = new SoundtrackMetadata();

            const string NAME = "Random O.S.T. - Song Name";
            const string DESCRIPTION = "nonsense";

            Track track = new()
            {
                Name = NAME,
                Description = DESCRIPTION
            };

            Assert.True(data.Fill(track, out Track modified));

            // should not modify original
            Assert.Equal(NAME, track.Name);
            Assert.Equal(DESCRIPTION, track.Description);

            Assert.Equal("Song Name", modified.Name);
            Assert.Equal("Random Soundtrack", modified.Album);
        }

        [Fact]
        public void Soundtrack_Name_3()
        {
            var data = new SoundtrackMetadata();

            const string NAME = "Random Soundtrack - Song Name";
            const string DESCRIPTION = "nonsense";

            Track track = new()
            {
                Name = NAME,
                Description = DESCRIPTION
            };

            Assert.True(data.Fill(track, out Track modified));

            // should not modify original
            Assert.Equal(NAME, track.Name);
            Assert.Equal(DESCRIPTION, track.Description);

            Assert.Equal("Song Name", modified.Name);
            Assert.Equal("Random Soundtrack", modified.Album);
        }

        [Fact]
        public void Name_Soundtrack_1()
        {
            var data = new SoundtrackMetadata();

            const string NAME = "Song Name - Random OST";
            const string DESCRIPTION = "nonsense";

            Track track = new()
            {
                Name = NAME,
                Description = DESCRIPTION
            };

            Assert.True(data.Fill(track, out Track modified));

            // should not modify original
            Assert.Equal(NAME, track.Name);
            Assert.Equal(DESCRIPTION, track.Description);

            Assert.Equal("Song Name", modified.Name);
            Assert.Equal("Random Soundtrack", modified.Album);
        }

        [Fact]
        public void Name_Soundtrack_2()
        {
            var data = new SoundtrackMetadata();

            const string NAME = "Song Name - Random O.S.T.";
            const string DESCRIPTION = "nonsense";

            Track track = new()
            {
                Name = NAME,
                Description = DESCRIPTION
            };

            Assert.True(data.Fill(track, out Track modified));

            // should not modify original
            Assert.Equal(NAME, track.Name);
            Assert.Equal(DESCRIPTION, track.Description);

            Assert.Equal("Song Name", modified.Name);
            Assert.Equal("Random Soundtrack", modified.Album);
        }

        [Fact]
        public void Name_Soundtrack_3()
        {
            var data = new SoundtrackMetadata();

            const string NAME = "Song Name - Random Soundtrack";
            const string DESCRIPTION = "nonsense";

            Track track = new()
            {
                Name = NAME,
                Description = DESCRIPTION
            };

            Assert.True(data.Fill(track, out Track modified));

            // should not modify original
            Assert.Equal(NAME, track.Name);
            Assert.Equal(DESCRIPTION, track.Description);

            Assert.Equal("Song Name", modified.Name);
            Assert.Equal("Random Soundtrack", modified.Album);
        }

        [Fact]
        public void IndexRemoval_1()
        {
            var data = new SoundtrackMetadata();

            const string NAME = "Random Soundtrack - 1 - Song Name";
            const string DESCRIPTION = "nonsense";

            Track track = new()
            {
                Name = NAME,
                Description = DESCRIPTION
            };

            Assert.True(data.Fill(track, out Track modified));

            // should not modify original
            Assert.Equal(NAME, track.Name);
            Assert.Equal(DESCRIPTION, track.Description);

            Assert.Equal("Song Name", modified.Name);
            Assert.Equal("Random Soundtrack", modified.Album);
        }

        [Fact] public void IndexRemoval_2()
        {
            var data = new SoundtrackMetadata();

            const string NAME = "Random Ost: 1 - Song Name";
            const string DESCRIPTION = "nonsense";

            Track track = new()
            {
                Name = NAME,
                Description = DESCRIPTION
            };

            Assert.True(data.Fill(track, out Track modified));

            // should not modify original
            Assert.Equal(NAME, track.Name);
            Assert.Equal(DESCRIPTION, track.Description);

            Assert.Equal("Song Name", modified.Name);
            Assert.Equal("Random Soundtrack", modified.Album);
        }

        [Fact]
        public void CompleteMess_1()
        {
            var data = new SoundtrackMetadata();

            const string NAME = "\"Random O.S.T. \" ( 3278 ) \"Song Name \"";
            const string DESCRIPTION = "nonsense";

            Track track = new()
            {
                Name = NAME,
                Description = DESCRIPTION
            };

            Assert.True(data.Fill(track, out Track modified));

            // should not modify original
            Assert.Equal(NAME, track.Name);
            Assert.Equal(DESCRIPTION, track.Description);

            Assert.Equal("Song Name", modified.Name);
            Assert.Equal("Random Soundtrack", modified.Album);
        }

        [Fact]
        public void ComplexIndex()
        {
            var data = new SoundtrackMetadata();

            const string NAME = "Random OST - Song Name (1-2)";
            const string DESCRIPTION = "nonsense";

            Track track = new()
            {
                Name = NAME,
                Description = DESCRIPTION
            };

            Assert.True(data.Fill(track, out Track modified));

            // should not modify original
            Assert.Equal(NAME, track.Name);
            Assert.Equal(DESCRIPTION, track.Description);

            Assert.Equal("Song Name", modified.Name);
            Assert.Equal("Random Soundtrack", modified.Album);
        }

        [Fact]
        public void NoNameTrack_1()
        {
            var data = new SoundtrackMetadata();

            const string NAME = "Track #1 - Random OST";
            const string DESCRIPTION = "nonsense";

            Track track = new()
            {
                Name = NAME,
                Description = DESCRIPTION
            };

            Assert.True(data.Fill(track, out Track modified));

            // should not modify original
            Assert.Equal(NAME, track.Name);
            Assert.Equal(DESCRIPTION, track.Description);

            Assert.Equal("Track #1 - Random Soundtrack", modified.Name);
            Assert.Equal("Random Soundtrack", modified.Album);
        }

        [Fact]
        public void NoNameTrack_2()
        {
            var data = new SoundtrackMetadata();

            const string NAME = "Random OST - Track #1";
            const string DESCRIPTION = "nonsense";

            Track track = new()
            {
                Name = NAME,
                Description = DESCRIPTION
            };

            Assert.True(data.Fill(track, out Track modified));

            // should not modify original
            Assert.Equal(NAME, track.Name);
            Assert.Equal(DESCRIPTION, track.Description);

            Assert.Equal("Track #1 - Random Soundtrack", modified.Name);
            Assert.Equal("Random Soundtrack", modified.Album);
        }

        [Fact]
        public void PrepositionWithNumber_1()
        {
            var data = new SoundtrackMetadata();

            const string NAME = "Up to 4 - Random OST";
            const string DESCRIPTION = "nonsense";

            Track track = new()
            {
                Name = NAME,
                Description = DESCRIPTION
            };

            Assert.True(data.Fill(track, out Track modified));

            // should not modify original
            Assert.Equal(NAME, track.Name);
            Assert.Equal(DESCRIPTION, track.Description);

            Assert.Equal("Up to 4", modified.Name);
            Assert.Equal("Random Soundtrack", modified.Album);
        }

        [Fact]
        public void NumberInMiddle()
        {
            var data = new SoundtrackMetadata();

            const string NAME = "Walking 4 Dogs - Random OST";
            const string DESCRIPTION = "nonsense";

            Track track = new()
            {
                Name = NAME,
                Description = DESCRIPTION
            };

            Assert.True(data.Fill(track, out Track modified));

            // should not modify original
            Assert.Equal(NAME, track.Name);
            Assert.Equal(DESCRIPTION, track.Description);

            Assert.Equal("Walking 4 Dogs", modified.Name);
            Assert.Equal("Random Soundtrack", modified.Album);
        }
    }
}
