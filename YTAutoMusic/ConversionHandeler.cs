using System.Diagnostics;
using System.Configuration;

namespace YTAutoMusic
{
    internal class ConversionHandeler
    {
        private readonly int MAX_PROCESS_COUNT;

        private readonly List<MusicBundle> bundles;
        private readonly Queue<string> argumentQueue;

        private readonly string ffmpegPath;

        public ConversionHandeler(IEnumerable<FileInfo> toConvert, string finalDirectory, string ffmpegPath)
        {
            this.ffmpegPath = ffmpegPath;

            Console.WriteLine("\nStarting Conversion.");

            MAX_PROCESS_COUNT = Math.Max(1, Environment.ProcessorCount / 2);
            Console.WriteLine($"Allowing {MAX_PROCESS_COUNT} processes\n");

            bundles = new(toConvert.Count());
            argumentQueue = new(toConvert.Count());

            string fileExtension = ConfigurationManager.AppSettings.Get("Output file extension");
            if(string.IsNullOrWhiteSpace(fileExtension))
            {
                throw new ConfigurationErrorsException("Invalid file extension");
            }

            string ffmpegAddition = ConfigurationManager.AppSettings.Get("Additional ffmpeg commands");
            if (string.IsNullOrWhiteSpace(fileExtension))
            {
                ffmpegAddition = "";
            }

            foreach (var sound in toConvert)
            {
                string originalName = sound.FullName;
                string rawName = PlaylistDownloader.GetNameWithoutURLTag(sound.Name);
                string newName = Path.Combine(finalDirectory, $"{rawName}.mp3");

                Console.WriteLine($"Queuing conversion: '{originalName}'\n");

                FileInfo newFile = new(newName);
                var bundle = new MusicBundle(newFile, PlaylistDownloader.GetURLTag(sound.Name), rawName, "");
                bundles.Add(bundle);

                argumentQueue.Enqueue($"-i \"{originalName}\" {ffmpegAddition} \"{newName}\"");
            }

            Console.WriteLine("\n");
        }

        public void Convert()
        {
            List<Task> tasks = new(MAX_PROCESS_COUNT);

            while (argumentQueue.Any() || tasks.Any())
            {
                tasks.RemoveAll(task => task.IsCompleted);

                if (tasks.Count < MAX_PROCESS_COUNT && argumentQueue.Any())
                {
                    Task task = ConvertIndividual(argumentQueue.Dequeue());
                    task.ConfigureAwait(false);
                    tasks.Add(task); 
                }

                Task.Delay(100);
            }
        }

        private async Task ConvertIndividual(string args)
        {
            Console.WriteLine($"\nConversion start: '{args}'\n");
            
            var ffmpeg = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = ffmpegPath,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            ffmpeg.Start();

            await ffmpeg.WaitForExitAsync();

            if(ffmpeg.ExitCode != 0)
            {
                Console.WriteLine($"\nAN ERROR HAS OCCURRED DURING CONVERSION! \n\"\n{ffmpeg.StandardOutput.ReadToEnd()}\n\"\n");
            }

            Console.WriteLine($"\nConversion complete: '{args}'\n");
        }

        public MusicBundle[] GetMusicBundles()
        {
            return bundles.ToArray();
        }
    }
}
