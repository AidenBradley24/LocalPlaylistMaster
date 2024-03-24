using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using LocalPlaylistMaster.Backend;

namespace LocalPlaylistMaster
{
    public partial class ExportPlaylistWindow : Window
    {
        private readonly DatabaseManager dbManager;
        private readonly Playlist playlist;
        private PlaylistExportManager? playlistManager;

        public ExportPlaylistWindow(Playlist playlist, DatabaseManager manager)
        {
            dbManager = manager;
            this.playlist = playlist;

            InitializeComponent();
            Task.Run(Setup);
        }

        private async Task Setup()
        {
            playlistManager = await dbManager.CreatePlaylistExport(playlist);
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

            if (playlistManager.InvalidTracks.Any())
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

            if (playlistManager.ValidTracks.Any())
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

        }
    }
}
