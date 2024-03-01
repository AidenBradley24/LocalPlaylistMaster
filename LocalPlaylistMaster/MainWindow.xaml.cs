using LocalPlaylistMaster.Backend;
using Microsoft.Win32;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

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
            get => Model.manager; 
            set => Model.manager = value;
        }

        internal MainWindowModel Model { get => DataContext as MainWindowModel ?? throw new Exception(); }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowModel(this);
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
            Model.RefreshTracks();
        }
        
        public void InitializePlaylist(PlaylistManager playlist)
        {
            PlaylistManager = playlist;
            Trace.WriteLine("YIPPEE");
        }

        private void OpenExistingPlaylist(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog openFolderDialog = new()
            {
                Multiselect = false,
                ValidateNames = true,
                DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                Title = "Select music database folder",
            };

            if (openFolderDialog.ShowDialog(this) ?? false)
            {
                DirectoryInfo dir = new(openFolderDialog.FolderName);
                if (!dir.Exists)
                {
                    MessageBox.Show("Directory doesn't exist", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                try
                {
                    PlaylistManager = new PlaylistManager(dir.FullName, dependencyProcessManager);
                    Model.RefreshTracks();
                }
                catch (Exception ex)
                {
                    PlaylistManager = null;
                    Model.RefreshTracks();
                    MessageBox.Show(ex.Message, "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void TrackGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Model.DisplaySelection(trackGrid.SelectedItems.Cast<Track>());
        }

        private void CancelItemUpdate(object sender, RoutedEventArgs e)
        {
            Model.CancelItemUpdate();
        }

        private void ConfirmItemUpdate(object sender, RoutedEventArgs e)
        {
            Model.ConfirmItemUpdate();            
        }

        private void CheckBox_DontAllowIndeterminate(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.IsThreeState && checkBox.IsChecked == true)
            {
                checkBox.IsChecked = false;
                e.Handled = true;
            }
        }
    }
}