using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using LocalPlaylistMaster.Backend;
using LocalPlaylistMaster.Backend.Utilities;
using System.Windows.Data;

namespace LocalPlaylistMaster
{
    public partial class NewRemoteWindow : Window
    {
        private readonly RemoteModel model;
        private readonly DatabaseManager manager;
        private readonly Dictionary<RemoteType, int> remoteTypeMap;

        public NewRemoteWindow(DatabaseManager manager)
        {
            InitializeComponent();
            model = new RemoteModel();
            DataContext = model;
            this.manager = manager;

            Binding autoType = new(nameof(RemoteModel.AutomaticTypeLabel));

            // this is terrible
            ComboBoxItem[] items =
            [
                new ComboBoxItem()
                {
                    Content = autoType,
                    Tag = RemoteType.UNINITIALIZED,
                },
                new ComboBoxItem()
                {
                    Content = "YouTube / web video playlist",
                    Tag = RemoteType.ytdlp_playlist,
                },
                new ComboBoxItem()
                {
                    Content = "YouTube / web video concert",
                    Tag = RemoteType.ytdlp_concert,
                },
            ];

            items[0].SetBinding(ContentProperty, autoType);

            remoteTypeMap = [];
            remoteTypeMap.Add(RemoteType.UNINITIALIZED, 0);
            remoteTypeMap.Add(RemoteType.ytdlp_playlist, 1);
            remoteTypeMap.Add(RemoteType.ytdlp_concert, 2);

            typeBox.ItemsSource = items;
            typeBox.SelectionChanged += TypeBox_SelectionChanged;
            model.PropertyChanged += Model_PropertyChanged;
            typeBox.SelectedIndex = remoteTypeMap[model.Type];
        }

        private void Model_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(RemoteModel.Type))
            {
                typeBox.SelectedIndex = remoteTypeMap[model.Type];
            }
        }

        private void TypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            model.Type = (RemoteType)((ComboBoxItem)typeBox.SelectedItem).Tag;
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
            Remote? r = model.Export();
            if (r == null)
            {
                return;
            }

            int newRemoteId = await manager.IngestRemote(r);

            var messageResult = MessageBox.Show("Do you want to sync now with the remote?", "Sync", MessageBoxButton.YesNo);
            if (messageResult == MessageBoxResult.Yes)
            {
                ProgressModel progressModel = new();
                ProgressDisplay progressDisplayWindow = new(progressModel);
                var reporter = progressModel.GetProgressReporter();
                progressDisplayWindow.Show();
                IsEnabled = false;
                Hide();

                await Task.Run(async () =>
                {
                    await manager.SyncRemote(newRemoteId, reporter);
                });

                IsEnabled = true;
                progressDisplayWindow.Close();
            }

            Close();
        }
    }
}
