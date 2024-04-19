using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using LocalPlaylistMaster.Backend;
using Microsoft.Win32;

namespace LocalPlaylistMaster
{
    public partial class ExportPlaylistWindow : Window, INotifyPropertyChanged
    {
        private readonly DatabaseManager dbManager;
        private readonly PlaylistExportManager playlistManager;
        public event PropertyChangedEventHandler? PropertyChanged;

        private string fileLocation;
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

        public ExportType MyExportType
        {
            get => playlistManager.Type;
            set
            {
                playlistManager.Type = value;
                OnPropertyChanged(nameof(MyExportType));
                OnPropertyChanged(nameof(CreationMessage));
            }
        }

        public ExportPlaylistWindow(Playlist playlist, DatabaseManager db)
        {
            DataContext = this;
            dbManager = db;
            playlistManager = new PlaylistExportManager()
            {
                Playlist = playlist,
            };
            MyExportType = ExportType.folder;
            fileLocation = "";
            InitializeComponent();
            Task.Run(Setup);
        }

        private async Task Setup()
        {
            await playlistManager.Setup(dbManager);
            Dispatcher.Invoke(WriteDoc);
        }

        private void WriteDoc()
        {
            if (playlistManager == null) return;

            Section section = new();

            Paragraph titleParagraph = new(new Run("Export Summary"))
            {
                FontSize = 20,
                Foreground = Brushes.Blue
            };

            section.Blocks.Add(titleParagraph);

            if (playlistManager.InvalidTracks?.Any() ?? false)
            {
                Paragraph paragraph = new(new Run("The following tracks are inelgible for export.\nThey must be downloaded first."))
                {
                    Margin = new Thickness(0, 5, 0, 0),
                    FontSize = 12,
                    Foreground = Brushes.Red
                };

                section.Blocks.Add(paragraph);

                List list = new()
                {
                    MarkerStyle = TextMarkerStyle.None,
                    Foreground = Brushes.Red
                };

                foreach(Track track in playlistManager.InvalidTracks)
                {
                    ListItem item = new(new Paragraph(new Run(track.ToString())));
                    list.ListItems.Add(item);
                }

                section.Blocks.Add(list);
            }

            if (playlistManager.ValidTracks?.Any() ?? false)
            {
                Paragraph paragraph = new(new Run("The following tracks will be included in the playlist."))
                {
                    Margin = new Thickness(0, 5, 0, 0),
                    FontSize = 12,
                    Foreground = Brushes.Green
                };
                section.Blocks.Add(paragraph);

                List list = new()
                {
                    MarkerStyle = TextMarkerStyle.None,
                    Foreground = Brushes.Green,
                };

                foreach (Track track in playlistManager.ValidTracks)
                {
                    ListItem item = new(new Paragraph(new Run(track.ToString())));
                    list.ListItems.Add(item);
                }

                section.Blocks.Add(list);
            }
            else
            {
                Paragraph paragraph = new(new Run("There were no valid tracks in the playlist!\nYou cannot export an empty playlist!"))
                {
                    Margin = new Thickness(0, 5, 0, 0),
                    FontSize = 12,
                    Foreground = Brushes.OrangeRed
                };
                section.Blocks.Add(paragraph);
            }

            Doc.Blocks.Add(section);
        }

        private void CancelButton(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void ExportButton(object sender, RoutedEventArgs e)
        {
            await Export();
        }

        private async Task Export()
        {
            if (string.IsNullOrWhiteSpace(FileLocation))
            {
                MessageBox.Show("Please fill in file location", "Unable to create playlist", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (FullPath == ERROR_PATH)
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

            playlistManager.OutputDir = new DirectoryInfo(FullPath);

            ProgressModel progressModel = new();
            ProgressDisplay progressDisplayWindow = new(progressModel);
            var reporter = progressModel.GetProgressReporter();
            progressDisplayWindow.Show();
            IsEnabled = false;

            await Task.Run(async () =>
            {
                await playlistManager.Export(reporter);
            });

            IsEnabled = true;
            progressDisplayWindow.Close();
            Close();
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SelectLocation(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog openFolderDialog = new()
            {
                Multiselect = false,
                ValidateNames = true,
                DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                Title = "Select output folder",
            };

            if (openFolderDialog.ShowDialog(this) ?? false)
            {
                FileLocation = openFolderDialog.FolderName;
            }
        }

        private const string ERROR_PATH = "Error!";
        private string FullPath
        {
            get
            {
                try
                {
                    string path = playlistManager.HasLib ? Path.GetFullPath(FileLocation) : Path.Join(Path.GetFullPath(FileLocation), Backend.Utilities.Extensions.CleanName(playlistManager.Playlist?.Name ?? throw new Exception()));
                    if (Backend.Utilities.Extensions.IsInsideProject(path))
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
    }
}
