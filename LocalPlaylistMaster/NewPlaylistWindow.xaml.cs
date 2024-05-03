using System.Windows;
using LocalPlaylistMaster.Backend;

namespace LocalPlaylistMaster
{
    public partial class NewPlaylistWindow : Window
    {
        private readonly PlaylistModel model;
        private readonly DatabaseManager manager;

        public NewPlaylistWindow(DatabaseManager manager)
        {
            InitializeComponent();
            model = new PlaylistModel();
            DataContext = model;
            this.manager = manager;
        }

        private void CancelButton(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void AddButton(object sender, RoutedEventArgs e)
        {
            await Add();
        }

        private async Task Add()
        {
            Playlist? p = model.Export();
            if (p == null)
            {
                return;
            }

            await manager.IngestPlaylist(p);
            Close();
        }
    }
}
