namespace YTAutoMusic
{
    internal static class PlaylistCopier
    {
        public static void Copy()
        {
            string folder;
            DirectoryInfo sourceDirectory;

            while (true)
            {
                Console.WriteLine("Provide existing playlist directory.\nIt should have xspf file and a 'tracks' folder inside.");
                folder = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(folder))
                {
                    continue;
                }

                try
                {
                    folder = Path.GetFullPath(folder);
                }
                catch
                {
                    Console.WriteLine($"{folder} is not a valid directory.");
                    continue;
                }

                if (!Directory.Exists(Path.Combine(folder, "tracks")))
                {
                    Console.WriteLine("Playlist directory does not have a 'tracks' directory inside.");
                    continue;
                }

                sourceDirectory = Directory.CreateDirectory(folder);

                if (PlaylistDownloader.IsInsideProject(sourceDirectory))
                {
                    Console.WriteLine("Cannot open here.");
                    continue;
                }

                break;
            }

            DirectoryInfo targetDirectory;

            while (true)
            {
                Console.WriteLine("Provide target root directory.");
                folder = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(folder))
                {
                    continue;
                }

                try
                {
                    folder = Path.GetFullPath(folder);
                }
                catch
                {
                    Console.WriteLine($"{folder} is not a valid directory.");
                    continue;
                }

                targetDirectory = Directory.CreateDirectory(Path.Combine(folder, sourceDirectory.Name));

                if (PlaylistDownloader.IsInsideProject(targetDirectory))
                {
                    Console.WriteLine("Cannot open here.");
                    continue;
                }

                break;
            }

            string descriptionPath = Path.Combine(sourceDirectory.FullName, "description.txt");
            if (!File.Exists(descriptionPath))
            {
                Console.WriteLine("Description file not found. Unable to copy.");
                return;
            }

            Console.WriteLine(targetDirectory.FullName);

            var descriptionFile = new FileInfo(descriptionPath);
            descriptionFile.CopyTo(Path.Combine(targetDirectory.FullName, "description.txt"));

            var sourceTracksDirectory = Directory.CreateDirectory(Path.Combine(sourceDirectory.FullName, "tracks"));
            var tracksDirectory = targetDirectory.CreateSubdirectory("tracks");

            foreach(FileInfo file in sourceTracksDirectory.GetFiles())
            {
                file.CopyTo(Path.Combine(tracksDirectory.FullName, file.Name));
            }

            (string title, string description, string id) = GetInfoFromDescription(descriptionFile);
            PlaylistBundle playlist = new(title, description, id);

            PlaylistDownloader.CreatePlaylistFiles(playlist, tracksDirectory);

            Console.WriteLine("Copying complete");
        }

        private static (string title, string description, string id) GetInfoFromDescription(FileInfo descriptionFile)
        {
            using var reader = descriptionFile.OpenText();
            string title = reader.ReadLine().Remove(0, "Title: ".Length);
            string id = reader.ReadLine().Remove(0, "ID: ".Length);
            string description = reader.ReadToEnd();
            description = description.Remove(description.IndexOf($"Playlist sourced from https://www.youtube.com/playlist?list={id}")).Trim();

            return (title, description, id);
        }
    }
}
