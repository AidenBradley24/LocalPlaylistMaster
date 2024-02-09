namespace LocalPlaylistMaster.Backend.Extensions
{
    public static class Extensions
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
    }
}
