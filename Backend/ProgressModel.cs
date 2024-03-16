using System.ComponentModel;
using System.Diagnostics;

namespace LocalPlaylistMaster.Backend
{
    /// <summary>
    /// Exposes progress and details of async background tasks
    /// </summary>
    public class ProgressModel : INotifyPropertyChanged
    {
        private string titleText = "";
        private string detailText = "";
        private int progress = 0;
        private MessageBox? message;

        public string TitleText
        {
            get => titleText;
            internal set
            {
                Trace.WriteLine($"TITLE {value}");
                titleText = value;
                OnPropertyChanged(nameof(TitleText));
            }
        }

        public string DetailText
        {
            get => detailText;
            internal set
            {
                Trace.WriteLine($"DETAIL {value}");
                detailText = value;
                OnPropertyChanged(nameof(DetailText));
            }
        }

        public int Progress
        {
            get => progress;
            internal set
            {
                progress = value;
                OnPropertyChanged(nameof(Progress));
            }
        }

        public MessageBox? Message
        {
            get => message;
            internal set
            {
                message = value;
                OnPropertyChanged(nameof(Message));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public enum ReportType { TitleText, DetailText, Progress, Message }

        public void Report(ReportType type, object report)
        {
            Trace.WriteLine($"{type}, {report}");
            switch(type)
            {
                case ReportType.TitleText:
                    TitleText = (string)report;
                    break;
                case ReportType.DetailText:
                    DetailText = (string)report;
                    break;
                case ReportType.Progress:
                    Progress = (int)report;
                    break;
                case ReportType.Message:
                    Message = (MessageBox?)report;
                    break;
            }
        }

        public Progress<(ReportType type, object report)> GetProgressReporter()
        {
            return new Progress<(ReportType type, object report)>((obj) =>
            {
                Report(obj.type, obj.report);
            });
        }

        public class MessageBox
        {
            public string Title { get; set; }
            public string Detail { get; set; }
            public MessageType Type { get; set; }

            public MessageBox()
            {
                Title = "";
                Detail = "";
                Type = MessageType.none;
            }

            public enum MessageType { error, warning, info, none }
        }
    }
}
