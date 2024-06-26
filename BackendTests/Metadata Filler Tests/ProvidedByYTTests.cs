﻿using LocalPlaylistMaster.Backend;
using LocalPlaylistMaster.Backend.Metadata_Fillers;
using Xunit;

namespace BackendTest.Metadata_Filler_Tests
{
    /// <summary>
    /// Test <see cref="ProvidedMetadata"/>
    /// </summary>
    public class ProvidedByYTTests
    {
        /*
         * Provided to YouTube by INSERTCOMPANYHERE
         * 
         * Title · Artist
         * 
         * Album
         * 
         * ℗
         * 
         * Released on: YYYY-MM-DD
         * 
         * ...
         * 
         * Auto-generated by YouTube.
         */

        [Fact]
        public void Basic()
        {
            var data = new ProvidedMetadata();

            const string NAME = "Title";
            const string DESCRIPTION = "Provided to YouTube by INSERTCOMPANYHERE\r\n\r\nTitle · Artist\r\n\r\nAlbum\r\n\r\n℗\r\n\r\nReleased on: 1234-10-05\r\n\r\n...\r\n\r\nAuto-generated by YouTube.";

            Track track = new()
            {
                Name = NAME,
                Description = DESCRIPTION
            };
             
            Assert.True(data.Fill(track, out Track modified));

            // should not modify original
            Assert.Equal(NAME, track.Name);
            Assert.Equal(DESCRIPTION, track.Description);

            // modified output
            Assert.Equal("Title", modified.Name);
            Assert.Equal("Artist", modified.Artists);
            Assert.Equal("Album", modified.Album);
        }
    }
}
