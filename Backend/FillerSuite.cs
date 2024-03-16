using System.Collections;

namespace LocalPlaylistMaster.Backend
{
    /// <summary>
    /// A collection of metadata fillers to run
    /// </summary>
    public abstract class FillerSuite : IEnumerable<MetadataBase>
    {
        protected abstract Type[] Fillers { get; }

        public IEnumerator<MetadataBase> GetEnumerator()
        {
            return Fillers.Select(t =>
            {
                return Activator.CreateInstance(t) as MetadataBase ?? throw new Exception();
            }).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
