using System.ComponentModel;
using LocalPlaylistMaster.Backend;

namespace LocalPlaylistMaster
{
    /// <summary>
    /// Creates and modifies playlists
    /// </summary>
    public class PlaylistModel : INotifyPropertyChanged
    {
        private readonly Playlist playlist;

        public string Name
        {
            get => playlist.Name;
            set
            {
                playlist.Name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public string Description
        {
            get => playlist.Description;
            set
            {
                playlist.Description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        public PlaylistModel(Playlist playlist)
        {
            this.playlist = playlist;
        }

        public PlaylistModel()
        {
            playlist = new();
        }

        public Playlist? Export()
        {
            return playlist;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
