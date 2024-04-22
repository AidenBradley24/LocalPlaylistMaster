using System.ComponentModel;
using System.Windows;
using LocalPlaylistMaster.Backend;

namespace LocalPlaylistMaster
{
    /// <summary>
    /// Creates and modifies remotes
    /// </summary>
    public class RemoteModel : INotifyPropertyChanged
    {
        private readonly Remote remote;

        public string Name
        {
            get => remote.Name;
            set
            {
                remote.Name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public string Description
        {
            get => remote.Description;
            set
            {
                remote.Description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        public string Link
        {
            get => remote.Link;
            set
            {
                remote.Link = value;
                OnPropertyChanged(nameof(Link));
                OnPropertyChanged(nameof(AutomaticTypeLabel));
            }
        }

        public RemoteType Type
        {
            get => remote.Type;
            set
            {
                remote.Type = value;
                OnPropertyChanged(nameof(Type));
            }
        }

        public string AutomaticTypeLabel
        {
            get
            {
                var auto = FindAutoType();
                string autoString;
                if (auto == RemoteType.UNINITIALIZED)
                {
                    autoString = "INVALID";
                }
                else
                {
                    autoString = auto.ToString();
                }

                return $"Automatic ({autoString})";
            }
        }

        private bool locked = false;

        public bool Locked
        {
            get => locked;
            set
            {
                locked = value;
                OnPropertyChanged(nameof(Locked));
            }
        }

        public RemoteModel(Remote remote)
        {
            this.remote = remote;
            Locked = remote.Settings.HasFlag(RemoteSettings.locked);
        }

        public RemoteModel()
        {
            remote = new();
        }

        public Remote? Export()
        {
            if(Type == RemoteType.UNINITIALIZED)
            {
                RemoteType newType = FindAutoType();
                if(newType == RemoteType.UNINITIALIZED)
                {
                    MessageBox.Show("Automatic type was unable to find a suitable type", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
                Type = newType;
            }

            RemoteSettings settings =
                remote.Settings.HasFlag(RemoteSettings.removeMe) ? RemoteSettings.removeMe : 0 |
                (Locked ? RemoteSettings.locked : 0);

            remote.Settings = settings;
            return remote;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public RemoteType FindAutoType()
        {
            if (string.IsNullOrEmpty(remote.Link))
            {
                return RemoteType.UNINITIALIZED;
            }
            else
            {
                return RemoteType.ytdlp_playlist;
            }
        }
    }
}
