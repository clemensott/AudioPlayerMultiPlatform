using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace AudioPlayerFrontend
{
    class WidthService
    {
        enum Behavior { Big, Medium, Small }

        private const double songTitleArtistMinWidth = 150, sliderTargetWidth = 200;

        private Behavior state;
        private TextBlock tblTitle, tblArtist;
        private ColumnDefinition cdSong;
        private FrameworkElement slider;

        public WidthService(TextBlock tblTitle, TextBlock tblArtist, ColumnDefinition cdSong, FrameworkElement slider)
        {
            this.tblTitle = tblTitle;
            this.tblArtist = tblArtist;
            this.cdSong = cdSong;
            this.slider = slider;

            var tblDPD = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
            tblDPD.AddValueChanged(tblTitle, OnReset);
            tblDPD.AddValueChanged(tblArtist, OnReset);

            var cdDPD = DependencyPropertyDescriptor.FromProperty(FrameworkElement.ActualWidthProperty, typeof(FrameworkElement));
            cdDPD.AddValueChanged(slider, OnUpdate);
        }

        private void OnReset(object sender, EventArgs e)
        {
            Reset();
        }

        private void OnUpdate(object sender, EventArgs e)
        {
            Update();
        }

        public void Reset()
        {
            double rawWidth = cdSong.ActualWidth + slider.ActualWidth;

            tblTitle.TextWrapping = TextWrapping.NoWrap;
            tblArtist.TextWrapping = TextWrapping.NoWrap;

            double maxSongWidth = WidthSmallerThan(double.PositiveInfinity);

            if (maxSongWidth < songTitleArtistMinWidth || rawWidth - maxSongWidth > sliderTargetWidth) state = Behavior.Big;
            else
            {
                tblTitle.TextWrapping = TextWrapping.Wrap;
                tblArtist.TextWrapping = TextWrapping.Wrap;

                if (rawWidth - sliderTargetWidth >= songTitleArtistMinWidth)
                {
                    maxSongWidth = WidthSmallerThan(rawWidth - sliderTargetWidth);
                    state = Behavior.Medium;
                }
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
                message += "\r\ncdSlider: " + slider.ActualWidth;

                MessageBox.Show(message, "WidthService.Reset");

                maxSongWidth = 0;
            }

            cdSong.Width = new GridLength(maxSongWidth);
        }

        public void Update()
        {
            switch (state)
            {
                case Behavior.Big:
                    if (slider.ActualWidth <= sliderTargetWidth) Reset();
                    return;

                case Behavior.Medium:
                    Reset();
                    return;

                case Behavior.Small:
                    if (slider.ActualWidth > sliderTargetWidth) Reset();
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
