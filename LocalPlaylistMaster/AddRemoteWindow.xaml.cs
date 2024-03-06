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
    public partial class AddRemoteWindow : Window
    {
        private readonly RemoteModel remoteModel;
        private readonly DatabaseManager playlistManager;

        public AddRemoteWindow(DatabaseManager manager)
        {
            InitializeComponent();
            remoteModel = new RemoteModel();
            DataContext = remoteModel;
            playlistManager = manager;
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
            Remote? r = remoteModel.Export();
            if (r == null)
            {
                return;
            }

            int newRemoteId = await playlistManager.IngestRemote(r);

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
                    await playlistManager.FetchRemote(newRemoteId, reporter);
                });

                IsEnabled = true;

                progressDisplayWindow.Close();
            }

            Close();
        }
    }
}
