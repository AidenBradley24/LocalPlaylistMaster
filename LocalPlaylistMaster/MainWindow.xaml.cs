using LocalPlaylistMaster.Backend;
using Microsoft.Win32;
using System.Collections;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.ComponentModel;

namespace LocalPlaylistMaster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        internal readonly DependencyProcessManager dependencyProcessManager;
        public DatabaseManager? DbManager 
        { 
            get => Model.Manager; 
            set => Model.Manager = value;
        }

        internal MainModel Model => DataContext as MainModel ?? throw new Exception();
        public TabItem CurrentTab => tabControl.SelectedItem as TabItem ?? throw new Exception();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainModel(this);

            try
            {
                dependencyProcessManager = new DependencyProcessManager();
                trackPlayer.ProcessManager = dependencyProcessManager;
            }
            catch (FileNotFoundException ex)
            {
                var result = MessageBox.Show(
                    $"Some needed binaries were not found.\n`{ex.Message}`\nThe program will attempt to download the needed binaries.", "Missing dependencies",
                    MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);
                
                if(result == MessageBoxResult.OK)
                {
                    try
                    {
                        DependencyProcessManager.DownloadProcesses();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show($"An error has occurred and download was not completed.\nPlease contact the developer. {e}");
                        Environment.Exit(-1);
                        return;
                    }
                    MessageBox.Show("Download complete!\nRestart the app.");
                    Environment.Exit(0);
                }
                else
                {
                    Environment.Exit(-1);
                }          
            }
        }

        private void CreateNewDb(object sender, RoutedEventArgs e)
        {
            NewDbWindow window = new(dependencyProcessManager)
            {
                Topmost = true,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            window.ShowDialog();
            Model.AddRecent(window.FullPath);
        }

        private void AddRemote(object sender, RoutedEventArgs e)
        {
            
        }
        
        public void InitializePlaylist(DatabaseManager playlist)
        {
            DbManager = playlist;
        }

        private void OpenExistingDb(object sender, RoutedEventArgs e)
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
                OpenExistingDb(openFolderDialog.FolderName);
            }
        }

        public void OpenExistingDb(string path)
        {
            DirectoryInfo dir = new(path);
            if (!dir.Exists)
            {
                MessageBox.Show("Directory doesn't exist", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                DbManager = new DatabaseManager(dir.FullName, dependencyProcessManager, false);
                Model.AddRecent(path);
                Model.RefreshAll();
            }
            catch (Exception ex)
            {
                DbManager = null;
                Model.RefreshAll();
                MessageBox.Show(ex.Message, "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool tabChanging = false;

        private void TrackGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tabChanging = false;
            Model.DisplaySelection(trackGrid.SelectedItems.Cast<Track>());
        }

        private void RemoteGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tabChanging = false;
            Model.DisplaySelection(remoteGrid.SelectedItems.Cast<Remote>());
        }

        private void PlaylistGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tabChanging = false;
            Model.DisplaySelection(playlistGrid.SelectedItems.Cast<Playlist>());
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

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabChanging)
            {
                Model.ClearSelection();            
            }
        }

        private void TabControl_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            tabChanging = true;
        }

        private void EditTrackQuery_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Model.EditPlaylistTracks();
        }

        private void TextBlockHoverHighlight_MouseEnter(object sender, MouseEventArgs e)
        {
            // Change the border brush color when the mouse enters the text block
            ((Border)((TextBlock)sender).Parent).BorderBrush = Brushes.Black;
        }

        private void TextBlockHoverHighlight_MouseLeave(object sender, MouseEventArgs e)
        {
            // Change the border brush color back when the mouse leaves the text block
            ((Border)((TextBlock)sender).Parent).BorderBrush = Brushes.Gray;
        }

        private void CloseApp(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            trackPlayer.Dispose();
        }
    }
}