using System.Windows;
using LocalPlaylistMaster.Backend;
using System.Windows.Controls;

namespace LocalPlaylistMaster
{
    public partial class UserQueryWindow : Window
    {
        private readonly UserQueryModel model;
        public UserQuery? Result { get => model.Export(); }

        public UserQueryWindow(UserQuery? userQuery)
        {
            InitializeComponent();
            model = userQuery == null ? new UserQueryModel() : new UserQueryModel(userQuery);
            DataContext = model;
        }

        private void CancelButton(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OkButton(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            model.QueryChanged(textBox);
        }
    }
}
