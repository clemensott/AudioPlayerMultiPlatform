using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

// Die Elementvorlage "Benutzersteuerelement" wird unter https://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

namespace AudioPlayerFrontend
{
    public sealed partial class AudioPositionSlider : UserControl
    {
        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.Register("Position", typeof(TimeSpan), typeof(AudioPositionSlider),
                new PropertyMetadata(TimeSpan.Zero, new PropertyChangedCallback(OnPositionPropertyChanged)));

        private static void OnPositionPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var s = (AudioPositionSlider)sender;
            var value = (TimeSpan)e.NewValue;

            if (s.isManipulatingSlider || s.isUpdatingSliderValue) return;

            s.isUpdatingSliderValue = true;
            s.sldPosition.Value = value.TotalSeconds;
            s.isUpdatingSliderValue = false;
        }

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register("Duration", typeof(TimeSpan), typeof(AudioPositionSlider),
                new PropertyMetadata(TimeSpan.Zero, new PropertyChangedCallback(OnDurationPropertyChanged)));

        private static void OnDurationPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var s = (AudioPositionSlider)sender;
            var value = (TimeSpan)e.NewValue;

            s.isUpdatingSliderValue = true;
            s.sldPosition.Maximum = value.TotalSeconds;
            s.isUpdatingSliderValue = false;
        }

        private bool isUpdatingSliderValue, isManipulatingSlider;

        public TimeSpan Position
        {
            get { return (TimeSpan)GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }

        public TimeSpan Duration
        {
            get { return (TimeSpan)GetValue(DurationProperty); }
            set { SetValue(DurationProperty, value); }
        }

        public AudioPositionSlider()
        {
            this.InitializeComponent();
        }

        private void SldPosition_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            isManipulatingSlider = true;
        }

        private void SldPosition_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            isManipulatingSlider = false;
            Position = TimeSpan.FromSeconds(sldPosition.Value);
        }

        private async void SldPosition_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (isUpdatingSliderValue || isManipulatingSlider) return;

            isUpdatingSliderValue = true;

            await Task.Delay(100);

            if (!isManipulatingSlider) Position = TimeSpan.FromSeconds(sldPosition.Value);

            isUpdatingSliderValue = false;
        }
    }
}
