using System.Windows;
using LocalPlaylistMaster.Backend;
using LocalPlaylistMaster.Backend.Utilities;

namespace LocalPlaylistMaster
{
    public partial class NewRemoteWindow : Window
    {
        private readonly RemoteModel model;
        private readonly DatabaseManager manager;

        public NewRemoteWindow(DatabaseManager manager)
        {
            InitializeComponent();
            model = new RemoteModel();
            DataContext = model;
            this.manager = manager;
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

                await Task.Run(async () =>
                {
                    await manager.FetchRemote(newRemoteId, reporter);
                });

                IsEnabled = true;

                progressDisplayWindow.Close();
            }

            Close();
        }
    }
}
