using System.ComponentModel;
using System.IO;
using System.Windows;
using LocalPlaylistMaster.Backend;
using LocalPlaylistMaster.Backend.Extensions;
using Microsoft.Win32;

namespace LocalPlaylistMaster
{
    public partial class NewDbWindow : Window, INotifyPropertyChanged
    {
        private string fileLocation;
        private string dbName;
        private readonly DependencyProcessManager dependencyProcessManager;

        public string DbName
        {
            get => dbName;

            set
            {
                dbName = value;
                OnPropertyChanged(nameof(DbName));
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
                    string path = Path.Join(Path.GetFullPath(FileLocation), DbName);
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

        public NewDbWindow(DependencyProcessManager dependencyProcessManager)
        {
            InitializeComponent();
            DataContext = this;
            dbName = "NewMusicDb";
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
                DatabaseManager manager = new(FullPath, dependencyProcessManager, true);
                manager.DbRecord.name = DbName;
                manager.UpdatePlaylistRecord();
                ((MainWindow)Owner).InitializePlaylist(manager);
            }
            catch
            {
                MessageBox.Show($"Unable to create playlist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Close();
            MessageBox.Show($"Created playlist {dbName} successfully!", "nice", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
