using LocalPlaylistMaster.Backend;
using System.ComponentModel;
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
        private readonly DependencyProcessManager dependencyProcessManager;
        public PlaylistManager? PlaylistManager 
        { 
            get => ((MainWindowModel)DataContext).manager; 
            set => ((MainWindowModel)DataContext).manager = value;
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowModel();
            dependencyProcessManager = new DependencyProcessManager();
        }

        private void CreateNewPlaylist(object sender, RoutedEventArgs e)
        {
            NewPlaylistWindow window = new(dependencyProcessManager)
            {
                Topmost = true,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            window.ShowDialog();
        }

        private void AddRemote(object sender, RoutedEventArgs e)
        {
            if(PlaylistManager == null)
            {
                MessageBox.Show("No playlist is open.\nCreate a playlist first.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            AddRemoteWindow window = new(PlaylistManager)
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
            PlaylistManager = playlist;
            Trace.WriteLine("YIPPEE");
        }
    }
}