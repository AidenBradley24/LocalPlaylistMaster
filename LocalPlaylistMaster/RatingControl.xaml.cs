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
    public partial class RatingControl : UserControl
    {
        public static readonly DependencyProperty RatingProperty = DependencyProperty.Register(
            "Rating", typeof(int?), typeof(RatingControl), new PropertyMetadata(-1, new PropertyChangedCallback(OnRatingPropertyChanged)));

        private static void OnRatingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RatingControl ratingControl = (RatingControl)d;
            ratingControl.UpdatePanel();
        }

        public int? Rating
        {
            get => (int?)GetValue(RatingProperty);
            set => SetValue(RatingProperty, value);
        }

        private readonly Image[] stars;

        public RatingControl()
        {
            InitializeComponent();
            stars = new Image[5];
            stars[0] = Star1;
            stars[1] = Star2;
            stars[2] = Star3;
            stars[3] = Star4;
            stars[4] = Star5;
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

        private void FillStars(int? rating)
        {
            if(rating == null)
            {
                Message.Text = "Varied";
                rating = -1;
            }
            else if (rating < 0)
            {
                Message.Text = "Unrated";
            }
            else
            {
                Message.Text = "";
            }

            for (int i = 0; i < stars.Length; i++)
            {
                if(rating < 0)
                {
                    stars[i].Source = GetImage(Mode.nr);
                }
                else if(rating == 0)
                {
                    stars[i].Source = GetImage(Mode.no);
                }
                else if(rating == 1)
                {
                    stars[i].Source = GetImage(Mode.half);
                    rating--;
                }
                else
                {
                    stars[i].Source = GetImage(Mode.whole);
                    rating -= 2;
                }
            }
        }

        private void UpdatePanel()
        {
            FillStars(Rating);
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
