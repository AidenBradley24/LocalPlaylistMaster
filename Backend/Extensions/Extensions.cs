using System.Text.RegularExpressions;

namespace LocalPlaylistMaster.Backend
{
    public static partial class Extensions
    {
        public static bool IsInsideProject(string fullPath)
        {
            var dir = new DirectoryInfo(fullPath);
            return IsInsideProject(dir);
        }

        public static bool IsInsideProject(DirectoryInfo targetDirectory)
        {
            var project = new FileInfo(Environment.ProcessPath).Directory;
            return IsInsideProject(targetDirectory, project);
        }

        private static bool IsInsideProject(DirectoryInfo targetDirectory, DirectoryInfo project)
        {
            if (targetDirectory.FullName == project.FullName)
            {
                return true;
            }

            var parent = targetDirectory.Parent;
            if (parent == null)
            {
                return false;
            }

            return IsInsideProject(parent, project);
        }

        /// <summary>
        /// Clean a name for use in the file system
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string CleanName(string name)
        {
            return CleanName().Replace(name, "").Trim();
        }

        [GeneratedRegex("[\\/:*?\"<>|]")]
        private static partial Regex CleanName();
    }
}
