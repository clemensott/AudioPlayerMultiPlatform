using AudioPlayerBackendLib;
using System;
using System.Windows.Threading;

namespace AudioPlayerFrontendWpf.ViewModel
{
    class DependentViewModel : ViewModelBase
    {
        private readonly DispatcherTimer timer;

        public IAudioService Service { get; private set; }

        public override TimeSpan Position
        {
            get { return Service.GetPositon(); }
            set
            {
                Service.SetPosition(value);

                OnNotifyPropertyChanged(nameof(Position));
            }
        }

        public override TimeSpan Duration
        {
            get { return Service.GetDuration(); }
            protected set { OnNotifyPropertyChanged(nameof(Duration)); }
        }

        public override PlayState PlayState
        {
            get { return Service.GetPlayState(); }
            set
            {
                Service.SetPlayState(value);

                OnNotifyPropertyChanged(nameof(PlayState));

                timer.IsEnabled = value == PlayState.Play;
            }
        }

        public override bool IsAllShuffle
        {
            get { return Service.GetIsAllShuffle(); }
            set
            {
                Service.SetIsAllShuffle(value);

                OnNotifyPropertyChanged(nameof(IsAllShuffle));
                OnNotifyPropertyChanged(nameof(IsShuffle));

                SetSongs();
            }
        }

        public override bool IsSearchShuffle
        {
            get { return Service.GetIsSearchShuffle(); }
            set
            {
                Service.SetIsSearchShuffle(value);

                OnNotifyPropertyChanged(nameof(IsSearchShuffle));
                OnNotifyPropertyChanged(nameof(IsShuffle));

                SetSongs();
            }
        }

        public override bool IsOnlySearch
        {
            get { return Service.GetIsOnlySearch(); }
            set
            {
                Service.SetIsOnlySearch(value);

                OnNotifyPropertyChanged(nameof(IsOnlySearch));

                SetSongs();
            }
        }

        public override string SearchKey
        {
            get { return Service.GetSearchKey(); }
            set
            {
                Service.SetSearchKey(value);

                OnNotifyPropertyChanged(nameof(SearchKey));
                OnNotifyPropertyChanged(nameof(IsSearching));
                OnNotifyPropertyChanged(nameof(IsShuffle));

                SetSongs();
            }
        }

        public override Song CurrentPlaySong
        {
            get { return Service.GetCurrentSong(); }
            set
            {
                Service.SetCurrentSong(value);

                OnNotifyPropertyChanged(nameof(CurrentViewSong));
                OnNotifyPropertyChanged(nameof(CurrentPlaySong));
            }
        }

        public override string[] Sources
        {
            get { return Service.GetMediaSources(); }
            set
            {
                Service.SetMediaSources(value);

                OnNotifyPropertyChanged(nameof(Sources));

                Refresh();
            }
        }

        public DependentViewModel()
        {
            Service = new AudioService();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += Timer_Tick;

            Service.GetMediaElement().MediaOpened += OnMediaOpened;
        }

        private void OnMediaOpened(object sender, System.Windows.RoutedEventArgs e)
        {
            OnNotifyPropertyChanged(nameof(CurrentViewSong));
            OnNotifyPropertyChanged(nameof(CurrentPlaySong));

            OnNotifyPropertyChanged(nameof(PositionFactor));
            OnNotifyPropertyChanged(nameof(Position));
            OnNotifyPropertyChanged(nameof(Duration));
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            OnNotifyPropertyChanged(nameof(PositionFactor));
            OnNotifyPropertyChanged(nameof(Position));
            OnNotifyPropertyChanged(nameof(Duration));
        }

        public override void Refresh()
        {
            Service.Refresh();

            SetSongs();
        }

        private void SetSongs()
        {
            Songs = IsSearching ? Service.GetSearchSongs() : Service.GetAllSongs();
        }

        public override void SetNextSong()
        {
            Service.Next();
        }

        public override void SetPreviousSong()
        {
            Service.Previous();
        }
    }
}
