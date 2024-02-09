using LocalPlaylistMaster.Backend;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LocalPlaylistMaster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        internal PlaylistManager? playlistManager;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void CreateNewPlaylist(object sender, RoutedEventArgs e)
        {
            NewPlaylistWindow window = new()
            {
                Topmost = true,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            window.ShowDialog();
        }
        
        public void InitializePlaylist(PlaylistManager playlist)
        {
            playlistManager = playlist;
            Trace.WriteLine("YIPPEE");
        }
    }
}