using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LocalPlaylistMaster
{
    /// <summary>
    /// Interaction logic for RatingControl.xaml
    /// </summary>
    public partial class RatingControl : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty RatingProperty = DependencyProperty.Register(
            "Rating", typeof(int), typeof(RatingControl), new PropertyMetadata(-1));

        public int Rating
        {
            get { return (int)GetValue(RatingProperty); }
            set { SetValue(RatingProperty, value); }
        }

        #region Stars
        private readonly ImageSource[] stars;
        public ImageSource Star1
        {
            get => stars[0];
            set
            {
                stars[0] = value;
                OnPropertyChanged(nameof(Star1));
            }
        }
        public ImageSource Star2
        {
            get => stars[1];
            set
            {
                stars[1] = value;
                OnPropertyChanged(nameof(Star2));
            }
        }
        public ImageSource Star3
        {
            get => stars[2];
            set
            {
                stars[2] = value;
                OnPropertyChanged(nameof(Star3));
            }
        }
        public ImageSource Star4
        {
            get => stars[3];
            set
            {
                stars[3] = value;
                OnPropertyChanged(nameof(Star4));
            }
        }
        public ImageSource Star5
        {
            get => stars[4];
            set
            {
                stars[4] = value;
                OnPropertyChanged(nameof(Star5));
            }
        }
        #endregion

        public RatingControl()
        {
            InitializeComponent();
            DataContext = this;
            stars = new ImageSource[5];
            Star1 = GetImage(Mode.whole);
            Star2 = GetImage(Mode.whole);
            Star3 = GetImage(Mode.whole);
            Star4 = GetImage(Mode.whole);
            Star5 = GetImage(Mode.whole);

            FillStars(3);
        }

        private ImageSource GetImage(Mode mode)
        {
            return (ImageSource)FindResource(mode switch
            {
                Mode.whole => "WholeStarIcon",
                Mode.half => "HalfStarIcon",
                Mode.no => "NoStarIcon",
                _ => "NoRatingStarIcon"
            });
        }
        public enum Mode { whole, half, no, nr }

        private void FillStars(int rating)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                if(rating < 0)
                {
                    stars[i] = GetImage(Mode.nr);
                }
                else if(rating == 0)
                {
                    stars[i] = GetImage(Mode.no);
                }
                else if(rating == 1)
                {
                    stars[i] = GetImage(Mode.half);
                    rating--;
                }
                else
                {
                    stars[i] = GetImage(Mode.whole);
                    rating -= 2;
                }
            }

            OnPropertyChanged(nameof(Star1));
            OnPropertyChanged(nameof(Star2));
            OnPropertyChanged(nameof(Star3));
            OnPropertyChanged(nameof(Star4));
            OnPropertyChanged(nameof(Star5));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void StarPanel_MouseMove(object sender, MouseEventArgs e)
        {
            Point position = e.GetPosition((IInputElement)sender);
            double mouseX = position.X;
            FillStars((int)Math.Round(mouseX / 10));
        }

        private void StarPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            FillStars(Rating);
        }

        private void StarPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Point position = e.GetPosition((IInputElement)sender);
                double mouseX = position.X;
                Rating = (int)Math.Round(mouseX / 10);
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                Rating = -1;
            }
        }
    }
}
