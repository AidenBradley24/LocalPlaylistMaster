using static LocalPlaylistMaster.Backend.ProgressModel;

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

            reporter.Report((ReportType.DetailText, $"Converting audio...\nUsing {MAX_PROCESS_COUNT} threads."));
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
            int completedTaskCount = 0;

            while (argumentQueue.Count != 0 || tasks.Count != 0)
            {
                int removedCount = tasks.RemoveAll(task => task.IsCompleted);
                if(removedCount > 0)
                {
                    completedTaskCount += removedCount;
                    int progressValue = (int)((float)completedTaskCount / totalTaskCount * 100);
                    reporter.Report((ReportType.Progress, progressValue));
                }

                if (tasks.Count < MAX_PROCESS_COUNT && argumentQueue.Count != 0)
                {
                    Task task = ConvertIndividual(argumentQueue.Dequeue());
                    await task.ConfigureAwait(false);
                    tasks.Add(task); 
                }

                await Task.Delay(100);
            }
        }

        private async Task ConvertIndividual(string args)
        {
            var ffmpeg = dependencyProcessManager.CreateFfmpegProcess();
            ffmpeg.StartInfo.Arguments = args;
            ffmpeg.StartInfo.CreateNoWindow = true;
            ffmpeg.Start();
            await ffmpeg.WaitForExitAsync();
        }
    }
}
