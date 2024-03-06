using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using LocalPlaylistMaster.Backend;
using LocalPlaylistMaster.Backend.Extensions;
using Microsoft.Win32;

namespace LocalPlaylistMaster
{
    public partial class NewPlaylistWindow : Window, INotifyPropertyChanged
    {
        private string fileLocation;
        private string playlistName;
        private DependencyProcessManager dependencyProcessManager;

        public string PlaylistName
        {
            get => playlistName;

            set
            {
                playlistName = value;
                OnPropertyChanged(nameof(PlaylistName));
                OnPropertyChanged(nameof(CreationMessage));
            }
        }

        public string FileLocation
        {
            get => fileLocation;

            set
            {
                fileLocation = value;
                OnPropertyChanged(nameof(FileLocation));
                OnPropertyChanged(nameof(CreationMessage));
            }
        }

        public string CreationMessage
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FileLocation))
                {
                    return "Please fill in file location";
                }
                
                return $"This will create a new playlist in {FullPath}";
            }
        }


        private const string ERROR_PATH = "Error!";
        private string FullPath { 
            get 
            {
                try
                {
                    string path = Path.Join(Path.GetFullPath(FileLocation), PlaylistName);
                    if (Extensions.IsInsideProject(path))
                    {
                        return ERROR_PATH;
                    }
                    return path;
                }
                catch
                {
                    return ERROR_PATH;
                }
            }  
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public NewPlaylistWindow(DependencyProcessManager dependencyProcessManager)
        {
            InitializeComponent();
            DataContext = this;
            playlistName = "New Playlist";
            fileLocation = "";
            this.dependencyProcessManager = dependencyProcessManager;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SelectLocation(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog openFolderDialog = new()
            {
                Multiselect = false,
                ValidateNames = true,
                DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                Title = "Select music database root folder",
            };

            if (openFolderDialog.ShowDialog(this) ?? false)
            {
                FileLocation = openFolderDialog.FolderName;
            }
        }

        private void CancelButton(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("CANCEL");
            Close();
        }

        private void CreateButton(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("CREATE");
            Create();
        }

        private void Create()
        {
            if (string.IsNullOrWhiteSpace(FileLocation))
            {
                MessageBox.Show("Please fill in file location", "Unable to create playlist", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if(FullPath == ERROR_PATH)
            {
                MessageBox.Show($"Unable to create playlist", "Invalid path", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DirectoryInfo dir = new(FullPath);
            if (!Directory.Exists(dir.Parent?.FullName))
            {
                MessageBox.Show($"Unable to create playlist in \"{FullPath}\"", "Directory does not exist", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                DatabaseManager manager = new(FullPath, dependencyProcessManager);
                manager.DbRecord.name = PlaylistName;
                manager.UpdatePlaylistRecord();
                ((MainWindow)Owner).InitializePlaylist(manager);
            }
            catch
            {
                MessageBox.Show($"Unable to create playlist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Close();
            MessageBox.Show($"Created playlist {playlistName} successfully!", "nice", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
