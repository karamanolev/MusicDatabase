using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for ReleaseScoreIndicator.xaml
    /// </summary>
    public partial class ReleaseScoreIndicator : UserControl
    {
        private int score = 0;

        public int Score
        {
            get { return this.score; }
            set
            {
                this.score = value;
                this.textScore.Text = this.score + " / 100";
                if (this.score == 0)
                {
                    this.column1.Width = new GridLength(0);
                    this.column2.Width = new GridLength(1, GridUnitType.Star);
                }
                else if (this.score == 100)
                {
                    this.column1.Width = new GridLength(1, GridUnitType.Star);
                    this.column2.Width = new GridLength(0);
                }
                else
                {
                    this.column1.Width = new GridLength(this.score, GridUnitType.Star);
                    this.column2.Width = new GridLength(100 - this.score, GridUnitType.Star);
                }
            }
        }

        public ReleaseScoreIndicator()
        {
            InitializeComponent();
        }
    }
}
