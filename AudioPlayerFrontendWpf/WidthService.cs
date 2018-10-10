using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AudioPlayerFrontendWpf
{
    class WidthService
    {
        enum Behavior { Big, Medium, Small }

        private const double songTitleArtistMinWidth = 150, sliderTargetWidth = 200;

        private Behavior state;
        private TextBlock tblTitle, tblArtist;
        private ColumnDefinition cdSong, cdSlider;

        public WidthService(TextBlock tblTitle, TextBlock tblArtist, ColumnDefinition cdSong, ColumnDefinition cdSlider)
        {
            this.tblTitle = tblTitle;
            this.tblArtist = tblArtist;
            this.cdSong = cdSong;
            this.cdSlider = cdSlider;
        }

        public void Reset()
        {
            double rawWidth = cdSong.ActualWidth + cdSlider.ActualWidth;

            tblTitle.TextWrapping = TextWrapping.NoWrap;
            tblArtist.TextWrapping = TextWrapping.NoWrap;

            double maxSongWidth = WidthSmallerThan(double.PositiveInfinity);

            if (maxSongWidth < songTitleArtistMinWidth || rawWidth - maxSongWidth > sliderTargetWidth) state = Behavior.Big;
            else
            {
                tblTitle.TextWrapping = TextWrapping.Wrap;
                tblArtist.TextWrapping = TextWrapping.Wrap;

                maxSongWidth = WidthSmallerThan(rawWidth - sliderTargetWidth);

                if (maxSongWidth >= songTitleArtistMinWidth) state = Behavior.Medium;
                else
                {
                    maxSongWidth = WidthSmallerThan(songTitleArtistMinWidth);

                    state = Behavior.Small;
                }
            }

            if (maxSongWidth < 0)
            {
                string message = "MaxSongWidth got beyond zero.";
                message += "\r\nMaxSongWidth: " + maxSongWidth;
                message += "\r\nState: " + state;
                message += "\r\ntblTitle: " + tblTitle.ActualWidth;
                message += "\r\ntblArtist: " + tblArtist.ActualWidth;
                message += "\r\ncdSong: " + cdSong.ActualWidth;
                message += "\r\ncdSlider: " + cdSlider.ActualWidth;

                MessageBox.Show(message, "WidthService.Reset");

                maxSongWidth = 0;
            }

            cdSong.Width = new GridLength(maxSongWidth);
            cdSlider.Width = new GridLength(1, GridUnitType.Star);

            //System.Diagnostics.Debug.WriteLine(state + ": " + maxSongWidth);
        }

        public void Update()
        {
            switch (state)
            {
                case Behavior.Big:
                    if (cdSlider.ActualWidth <= sliderTargetWidth) Reset();
                    return;

                case Behavior.Medium:
                    Reset();
                    return;

                case Behavior.Small:
                    if (cdSlider.ActualWidth > sliderTargetWidth) Reset();
                    return;
            }
        }

        private double WidthSmallerThan(double maxWidth)
        {
            tblTitle.Measure(Size(maxWidth));
            tblArtist.Measure(Size(maxWidth));

            return Math.Max(DesiredWidth(tblTitle), DesiredWidth(tblArtist));
        }

        private Size Size(double width)
        {
            return new Size(width, double.PositiveInfinity);
        }

        private double DesiredWidth(TextBlock tbl)
        {
            //return tbl.Margin.Left + tbl.Margin.Right;
            return tbl.DesiredSize.Width + tbl.Margin.Left + tbl.Margin.Right;
        }
    }
}
