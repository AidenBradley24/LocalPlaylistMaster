using System.Globalization;

namespace LocalPlaylistMaster.Backend.Utilities
{
    /// <summary>
    /// ffprobe utilities
    /// </summary>
    /// <param name="file"></param>
    /// <param name="track"></param>
    /// <param name="manager"></param>
    public class TrackProbe(FileInfo file, Track track, DependencyProcessManager manager)
    {
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
            track.TimeInSeconds = double.Parse(durationString, CultureInfo.InvariantCulture);
        }
    }
}
