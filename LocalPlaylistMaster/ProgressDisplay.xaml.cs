using LocalPlaylistMaster.Backend.Utilities;
using System.Windows;

namespace LocalPlaylistMaster
{
    /// <summary>
    /// Interaction logic for ProgressDisplay.xaml
    /// </summary>
    public partial class ProgressDisplay : Window
    {
        private readonly ProgressModel progressModel;
        private bool messageVisible = false;
        private bool closing = false;

        public ProgressDisplay(ProgressModel model)
        {
            InitializeComponent();
            Topmost = true;
            DataContext = model;
            progressModel = model;
            model.PropertyChanged += Model_PropertyChanged;
        }

        private void Model_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(ProgressModel.Message) && progressModel.Message != null)
            {
                messageVisible = true;
                MessageBox.Show(progressModel.Message.Detail, progressModel.Message.Title, MessageBoxButton.OK, progressModel.Message.Type switch
                {
                    ProgressModel.MessageBox.MessageType.info => MessageBoxImage.Information,
                    ProgressModel.MessageBox.MessageType.warning => MessageBoxImage.Warning,
                    ProgressModel.MessageBox.MessageType.error => MessageBoxImage.Error,
                    _ => MessageBoxImage.None,
                },MessageBoxResult.OK,MessageBoxOptions.None);
                messageVisible = false;
                if(closing) Close();
            }
            else if(e.PropertyName == nameof(ProgressModel.Progress))
            {
                if(progressModel.Progress < 0)
                {
                    progressBar.IsIndeterminate = true;
                }
                else
                {
                    progressBar.IsIndeterminate = false;
                    progressBar.Value = progressModel.Progress;
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            closing = true;
            if(messageVisible)
            {
                e.Cancel = true;
            }
        }
    }
}
