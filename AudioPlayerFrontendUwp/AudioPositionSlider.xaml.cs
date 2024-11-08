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
            DependencyProperty.Register(nameof(Position), typeof(TimeSpan), typeof(AudioPositionSlider),
                new PropertyMetadata(TimeSpan.Zero, new PropertyChangedCallback(OnPositionPropertyChanged)));

        private static void OnPositionPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var s = (AudioPositionSlider)sender;
            var value = (TimeSpan)e.NewValue;

            if (s.isManipulatingSlider || s.isUpdatingSliderValue) return;

            try
            {
                s.isUpdatingSliderValue = true;
                s.sldPosition.Value = value.TotalSeconds;
            }
            finally
            {
                s.isUpdatingSliderValue = false;
            }
        }

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register(nameof(Duration), typeof(TimeSpan), typeof(AudioPositionSlider),
                new PropertyMetadata(TimeSpan.Zero, new PropertyChangedCallback(OnDurationPropertyChanged)));

        private static void OnDurationPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var s = (AudioPositionSlider)sender;
            var value = (TimeSpan)e.NewValue;

            try
            {
                s.isUpdatingSliderValue = true;
                s.sldPosition.Maximum = value.TotalSeconds;
            }
            finally
            {
                s.isUpdatingSliderValue = false;
            }
        }

        private bool isUpdatingSliderValue, isManipulatingSlider;

        public event EventHandler<TimeSpan> UserPositionChanged;

        public TimeSpan Position
        {
            get => (TimeSpan)GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }

        public TimeSpan Duration
        {
            get => (TimeSpan)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
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

            UserPositionChanged?.Invoke(this, TimeSpan.FromSeconds(sldPosition.Value));
        }

        private async void SldPosition_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (isUpdatingSliderValue || isManipulatingSlider) return;

            try
            {
                isUpdatingSliderValue = true;

                await Task.Delay(100);

                if (!isManipulatingSlider) UserPositionChanged?.Invoke(this, TimeSpan.FromSeconds(sldPosition.Value));
            }
            finally
            {
                isUpdatingSliderValue = false;
            }
        }
    }
}
