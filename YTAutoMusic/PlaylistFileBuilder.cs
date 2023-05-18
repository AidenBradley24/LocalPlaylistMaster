using System.Configuration;
using System.Reflection;
using YTAutoMusic;

namespace YTAutoMusic
{
    internal class PlaylistFileBuilder
    {
        public PlaylistBundle PlaylistBundle { get; set; }
        public DirectoryInfo TrackDirectory { get; set; }

        private readonly IEnumerable<PlaylistFile> playlistFileOptions;

        public PlaylistFileBuilder()
        {
            playlistFileOptions = GetAllPlaylistFiles().Where((f) =>
            {
                var config = ConfigurationManager.AppSettings.Get($"Create {f.ConfigName}");
                if (config == null) return false;
                return config.Equals("true", StringComparison.InvariantCultureIgnoreCase);
            });
        }

        private static IEnumerable<PlaylistFile> GetAllPlaylistFiles()
        {
            List<PlaylistFile> files = new();
            IEnumerable<Type> metadataFillerTypes = Assembly.GetAssembly(typeof(PlaylistFile)).GetTypes().
                Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(PlaylistFile)));

            foreach (Type type in metadataFillerTypes)
            {
                files.Add((PlaylistFile)Activator.CreateInstance(type));
            }

            return files;
        }

        public bool IsValid()
        {
            return PlaylistBundle != null && TrackDirectory != null;
        }

        public void Build(DirectoryInfo targetDirectory)
        {
            if (!IsValid())
            {
                throw new InvalidOperationException("Playlist builder missing information");
            }

            foreach(var file in playlistFileOptions)
            {
                file.Build(targetDirectory, TrackDirectory, PlaylistBundle);
            }
        }
    }
}

/// <summary>
/// A generator for a playlist file
/// </summary>
internal abstract class PlaylistFile
{
    /// <summary>
    /// Name in the config file
    /// "Create %Playlist Name%"
    /// </summary>
    public abstract string ConfigName { get; }

    /// <summary>
    /// Create the playlist file.
    /// </summary>
    /// <param name="targetDirectory">Directory to place the file</param>
    /// <param name="trackDirectory">Location of the tagged audio files</param>
    /// <param name="bundle">Playlist information</param>
    public abstract void Build(DirectoryInfo targetDirectory, DirectoryInfo trackDirectory, PlaylistBundle bundle);
}