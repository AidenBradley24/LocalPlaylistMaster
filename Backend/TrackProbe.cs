namespace LocalPlaylistMaster.Backend
{
    public class TrackProbe
    {
        private readonly DependencyProcessManager manager;
        private readonly Track track;
        private readonly FileInfo file;

        public TrackProbe(FileInfo file, Track track, DependencyProcessManager manager)
        {
            this.file = file;
            this.manager = manager;
            this.track = track;
        }

        public async Task MatchDuration()
        {
            using var process = manager.CreateFFprobeProcess();
            process.StartInfo.Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{file.FullName}\"";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            await process.WaitForExitAsync();

            string durationString = process.StandardOutput.ReadToEnd().Trim();
            int seconds = (int)Math.Round(double.Parse(durationString));
            track.TimeInSeconds = seconds;
        }

        public double GetDuration()
        {
            using var process = manager.CreateFFprobeProcess();
            process.StartInfo.Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{file.FullName}\"";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            process.WaitForExit();

            string durationString = process.StandardOutput.ReadToEnd().Trim();
            return double.Parse(durationString);
        }
    }
}
