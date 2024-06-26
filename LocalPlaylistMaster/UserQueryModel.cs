﻿using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;
using LocalPlaylistMaster.Backend;

namespace LocalPlaylistMaster
{
    public class UserQueryModel : INotifyPropertyChanged
    {
        private UserQuery? userQuery;
        private string query;
        private string message;
        private string messageColor;

        public string Query
        {
            get => query;
            set
            {
                query = value;
                OnPropertyChanged(nameof(Query));
            }
        }

        public string Message
        {
            get => message;       
            private set
            {
                message = value;
                OnPropertyChanged(nameof(Message));
            }
        }
        public string MessageColor
        {
            get => messageColor; 
            private set
            {
                messageColor = value;
                OnPropertyChanged(nameof(MessageColor));
            }
        }
        public bool IsValid
        {
            get => userQuery != null;
        }

        public UserQueryModel(UserQuery existingQuery)
        {
            Message = "Select all tracks from the database";
            MessageColor = "Green";
            Query = existingQuery.Query;
        }

        public UserQueryModel()
        {
            Message = "Select all tracks from the database";
            MessageColor = "Green";
            Query = "";
        }

        public UserQuery? Export()
        {
            return userQuery;
        }

        public void QueryChanged(TextBox textBox)
        {
            try
            {
                userQuery = new UserQuery(textBox.Text);
            }
            catch (InvalidUserQueryException ex)
            {
                Message = $"Invalid query: {ex.Message}";
                MessageColor = "Red";
                userQuery = null;
            }
            catch (Exception ex)
            {
                Message = $"CRITICAL ERROR: {ex.Message}";
                MessageColor = "Red";
                userQuery = null;
            }

            if (!IsValid)
            {
                textBox.BorderBrush = Brushes.Red;
            }
            else
            {
                textBox.ClearValue(Border.BorderBrushProperty);
                MessageColor = "Green";

                if (string.IsNullOrWhiteSpace(userQuery?.Query))
                {
                    Message = "Select all tracks from the database";
                }
                else
                {
                    Message = "Query good!";
                }
            }

            OnPropertyChanged(nameof(IsValid));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
