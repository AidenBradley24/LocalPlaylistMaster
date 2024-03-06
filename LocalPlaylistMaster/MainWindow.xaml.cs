﻿using LocalPlaylistMaster.Backend;
using Microsoft.Win32;
using System.Collections;
using System.Data;
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

        internal MainWindowModel Model => DataContext as MainWindowModel ?? throw new Exception();
        public TabItem CurrentTab => tabControl.SelectedItem as TabItem ?? throw new Exception();

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
            Model.RefreshAll();
        }
        
        public void InitializePlaylist(PlaylistManager playlist)
        {
            PlaylistManager = playlist;
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
                    Model.RefreshAll();
                }
                catch (Exception ex)
                {
                    PlaylistManager = null;
                    Model.RefreshAll();
                    MessageBox.Show(ex.Message, "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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

        private void DownloadAudioButton(object sender, RoutedEventArgs e)
        {
            Model.DownloadSelectedTracks();
        }

        private void FetchAll(object sender, RoutedEventArgs e)
        {
            Model.FetchAll();
        }

        private void DownloadAll(object sender, RoutedEventArgs e)
        {
            Model.DownloadAll();
        }

        private void SyncAll(object sender, RoutedEventArgs e)
        {
            Model.SyncAll();
        }
    }
}