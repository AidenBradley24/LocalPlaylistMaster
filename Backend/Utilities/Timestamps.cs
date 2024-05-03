using System.Globalization;

namespace LocalPlaylistMaster.Backend.Utilities
{
    public static class Timestamps
    {
        public static TimeSpan ParseTime(string stamp)
        {
            stamp = stamp.Trim();
            int hours = 0;
            int minutes;
            int seconds;
            int milliseconds = 0;

            try
            {
                int i = stamp.IndexOf('.');
                if (i >= 0)
                {
                    milliseconds = int.Parse(stamp[..i].Trim(), CultureInfo.InvariantCulture);
                    stamp = stamp[..i].Trim();
                }

                string[] sections = stamp.Split(':');
                if (sections.Length == 2)
                {
                    minutes = int.Parse(sections[0], CultureInfo.InvariantCulture);
                    seconds = int.Parse(sections[1], CultureInfo.InvariantCulture);
                }
                else if (sections.Length == 3)
                {
                    hours = int.Parse(sections[0], CultureInfo.InvariantCulture);
                    minutes = int.Parse(sections[1], CultureInfo.InvariantCulture);
                    seconds = int.Parse(sections[2], CultureInfo.InvariantCulture);
                }
                else
                {
                    throw new FormatException("Invalid time.");
                }
            }
            catch (FormatException)
            {
                throw new FormatException("Invalid time.");
            }

            return new TimeSpan(0, hours, minutes, seconds, milliseconds);
        }

        public static string DisplayTime(TimeSpan time)
        {
            if (time > TimeSpan.FromHours(1))
            {
                return time.ToString(@"hh\:mm\:ss");
            }

            return time.ToString(@"mm\:ss");
        }
    }
}
