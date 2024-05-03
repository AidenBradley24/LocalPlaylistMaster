using System.Diagnostics;

namespace LocalPlaylistMaster.Backend
{
    public class DependencyProcessManager
    {
        private readonly string dlpPath, ffmpegPath, ffplayPath, ffprobePath;

        public DependencyProcessManager()
        {
            FileInfo processPath = new(Environment.ProcessPath ?? throw new ApplicationException("Unable to get process path."));
            DirectoryInfo directory = new(Path.Combine(
                processPath.Directory?.FullName ?? throw new ApplicationException("Unable to get process path."),
                "Dependencies", "bin"));

            dlpPath = Path.Combine(directory.FullName, "yt-dlp.exe");

            if (!File.Exists(dlpPath))
            {
                throw new FileNotFoundException("Unable to find yt-dlp.exe");
            }

            ffmpegPath = Path.Combine(directory.FullName, "ffmpeg.exe");

            if (!File.Exists(ffmpegPath))
            {
                throw new FileNotFoundException("Unable to find ffmpeg.exe");
            }

            ffplayPath = Path.Combine(directory.FullName, "ffplay.exe");

            if(!File.Exists(ffplayPath))
            {
                throw new FileNotFoundException("Unable to find ffplay.exe");
            }

            ffprobePath = Path.Combine(directory.FullName, "ffprobe.exe");

            if (!File.Exists(ffprobePath))
            {
                throw new FileNotFoundException("Unable to find ffprobe.exe");
            }
        }

        public static void DownloadProcesses()
        {
            FileInfo processPath = new(Environment.ProcessPath ?? throw new ApplicationException("Unable to get process path."));
            DirectoryInfo directory = processPath.Directory ?? throw new ApplicationException("Unable to get process path.");
            FileInfo download = new(Path.Combine(directory.FullName, "Dependencies", "fetch_all.bat"));

            ProcessStartInfo info = new()
            {
                FileName = "cmd.exe",
                Arguments = $"/C \"{download.FullName}\"",
                UseShellExecute = false,
                CreateNoWindow = false,
            };

            using Process process = Process.Start(info) ?? throw new ApplicationException("Unable to run download");
            process.WaitForExit();
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

        public Process CreateFFprobeProcess()
        {
            var p = new Process();
            p.StartInfo.FileName = ffprobePath;
            return p;
        }

        public Process CreateFFplayProcess()
        {
            var p = new Process();
            p.StartInfo.FileName = ffplayPath;
            return p;
        }
    }
}