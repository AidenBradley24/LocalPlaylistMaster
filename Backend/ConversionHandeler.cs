using static LocalPlaylistMaster.Backend.Utilities.ProgressModel;

namespace LocalPlaylistMaster.Backend
{
    internal class ConversionHandeler
    {
        public static readonly string TARGET_FILE_EXTENSION = "mp3";
        private readonly int MAX_PROCESS_COUNT;
        private readonly Queue<string> argumentQueue;
        private readonly DependencyProcessManager dependencyProcessManager;
        private readonly IProgress<(ReportType type, object report)> reporter;

        public ConversionHandeler(IEnumerable<(int id, string remoteId)> tracks, Dictionary<string, FileInfo> fileMap, DirectoryInfo finalDir,
            DependencyProcessManager dependencies, IProgress<(ReportType type, object report)> reporter)
        {
            this.reporter = reporter;
            dependencyProcessManager = dependencies;
            MAX_PROCESS_COUNT = Math.Max(1, Environment.ProcessorCount / 2);

            reporter.Report((ReportType.DetailText, $"Converting audio...\nUsing a max of {MAX_PROCESS_COUNT} processes."));
            reporter.Report((ReportType.Progress, 0));

            argumentQueue = new();
            foreach ((int id, string remoteId) in tracks)
            {
                string originalName = fileMap[remoteId].FullName;
                string newName = Path.Combine(finalDir.FullName, $"{id}.{TARGET_FILE_EXTENSION}");
                argumentQueue.Enqueue($"-i \"{originalName}\" \"{newName}\" -y");
            }
        }

        public async Task Convert()
        {
            List<Task> tasks = new(MAX_PROCESS_COUNT);
            int totalTaskCount = argumentQueue.Count;
            reporter.Report((ReportType.Progress, -1));

            while (argumentQueue.Count > 0 || tasks.Count > 0)
            {
                while (tasks.Count < MAX_PROCESS_COUNT && argumentQueue.Count > 0)
                {
                    string arg = argumentQueue.Dequeue();
                    Task task = Task.Run(async () =>
                    {
                        var ffmpeg = dependencyProcessManager.CreateFfmpegProcess();
                        ffmpeg.StartInfo.Arguments = arg;
                        ffmpeg.StartInfo.CreateNoWindow = true;
                        ffmpeg.Start();
                        await ffmpeg.WaitForExitAsync();
                    });

                    tasks.Add(task);
                }

                Task completedTask = await Task.WhenAny(tasks);
                tasks.Remove(completedTask);

                int completedTaskCount = totalTaskCount - argumentQueue.Count - tasks.Count;
                int progressValue = (int)((float)completedTaskCount / totalTaskCount * 100);
                reporter.Report((ReportType.Progress, progressValue));
            }
        }
    }
}
