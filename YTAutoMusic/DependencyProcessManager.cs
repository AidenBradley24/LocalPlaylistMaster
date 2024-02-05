using System.Diagnostics;

namespace LocalPlaylistMaster.Backend
{
    public class DependencyProcessManager
    {
        private readonly string dlpPath, ffmpegPath;

        public DependencyProcessManager()
        {
            FileInfo processPath = new(Environment.ProcessPath);
            DirectoryInfo directory = processPath.Directory;

            dlpPath = Path.Combine(directory.FullName, "Dependencies", "yt-dlp.exe");

            if (!File.Exists(dlpPath))
            {
                Console.WriteLine("Unable to find yt-dlp.exe.");
                Console.WriteLine($"File should be located in \"{dlpPath}\"");
                throw new FileNotFoundException("Unable to find yt-dlp.exe.");
            }

            ffmpegPath = Path.Combine(directory.FullName, "Dependencies", "ffmpeg.exe");

            if (!File.Exists(ffmpegPath))
            {
                Console.WriteLine("Unable to find ffmpeg.exe.");
                Console.WriteLine($"File should be located in \"{ffmpegPath}\"");
                throw new FileNotFoundException("Unable to find ffmpeg.exe.");
            }
        }

        public Process CreateDlpProcess()
        {
            var p = new Process();
            p.StartInfo.FileName = dlpPath;
            return p;
        }

        public Process CreateFfmpegProcess()
        {
            var p = new Process();
            p.StartInfo.FileName = ffmpegPath;
            return p;
        }
    }
}