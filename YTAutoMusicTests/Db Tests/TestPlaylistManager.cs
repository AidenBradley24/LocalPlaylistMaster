using LocalPlaylistMaster.Backend;
using Xunit.Abstractions;

namespace BackendTest.Db_Tests
{
    public class TestPlaylistManager
    {
        private const string TEST_PLAYLIST = "https://www.youtube.com/playlist?list=PL9_MsX_jOXI-PC5qz-p1XChA-iW4ntAM1";
        private readonly ITestOutputHelper output;

        public TestPlaylistManager(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void Test_Dependencies()
        {
            DependencyProcessManager dependency = new();
            
            var dlp = dependency.CreateDlpProcess();
            Assert.True(dlp != null);

            var ffmpeg = dependency.CreateFfmpegProcess();
            Assert.True(ffmpeg != null);
        }

        [Fact]
        public void Test_CreateDb()
        {
            DirectoryInfo tempDir = Directory.CreateTempSubdirectory("TEST_CREATEDB_");
            output.WriteLine(tempDir.FullName);
            DependencyProcessManager dependency = new();
            PlaylistManager playlistManager = new(tempDir.FullName, dependency);
            Assert.True(playlistManager != null);
        }

        [Fact]
        public void Test_AddRemote()
        {
            DirectoryInfo tempDir = Directory.CreateTempSubdirectory("TEST_ADDREMOTE_");
            output.WriteLine(tempDir.FullName);
            DependencyProcessManager dependency = new();
            PlaylistManager playlistManager = new(tempDir.FullName, dependency);
            playlistManager.IngestRemote(new Remote(Remote.UNINITIALIZED, "", "", TEST_PLAYLIST, Remote.UNINITIALIZED, RemoteType.ytdlp, RemoteSettings.none)).Wait();
        }

        [Fact]
        public void Test_FetchRemote()
        {
            DirectoryInfo tempDir = Directory.CreateTempSubdirectory("TEST_FetchRemote_");
            output.WriteLine(tempDir.FullName);
            DependencyProcessManager dependency = new();
            PlaylistManager playlistManager = new(tempDir.FullName, dependency);
            playlistManager.IngestRemote(new Remote(Remote.UNINITIALIZED, "", "", TEST_PLAYLIST, Remote.UNINITIALIZED, RemoteType.ytdlp, RemoteSettings.none)).Wait();
            playlistManager.FetchRemote(1).Wait(); // id starts at 1
        }

        [Fact]
        public void Test_ReadTracks()
        {
            DirectoryInfo tempDir = Directory.CreateTempSubdirectory("TEST_ReadTracks_");
            output.WriteLine(tempDir.FullName);
            DependencyProcessManager dependency = new();
            PlaylistManager playlistManager = new(tempDir.FullName, dependency);
            playlistManager.IngestRemote(new Remote(Remote.UNINITIALIZED, "", "", TEST_PLAYLIST, Remote.UNINITIALIZED, RemoteType.ytdlp, RemoteSettings.none)).Wait();
            playlistManager.FetchRemote(1).Wait(); // id starts at 1
            var tracks = playlistManager.GetTracks();
            tracks.Wait();
            output.WriteLine(string.Join(';', tracks.Result));
        }
    }
}
