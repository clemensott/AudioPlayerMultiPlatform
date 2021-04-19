using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace AudioPlayerBackend.Audio
{
    public class Playlist : IPlaylist, IEquatable<Playlist>
    {
        private string name;
        private OrderType shuffle;
        private LoopType loop;
        private TimeSpan position, duration;
        private Song? currentSong;
        private RequestSong? wannaSong;
        private Song[] songs;
        private IEnumerable<Song> allSongs;
        private readonly IInvokeDispatcherHelper helper;

        public event EventHandler<ValueChangedEventArgs<string>> NameChanged;
        public event EventHandler<ValueChangedEventArgs<OrderType>> ShuffleChanged;
        public event EventHandler<ValueChangedEventArgs<LoopType>> LoopChanged;
        public event EventHandler<ValueChangedEventArgs<TimeSpan>> PositionChanged;
        public event EventHandler<ValueChangedEventArgs<TimeSpan>> DurationChanged;
        public event EventHandler<ValueChangedEventArgs<Song?>> CurrentSongChanged;
        public event EventHandler<ValueChangedEventArgs<RequestSong?>> WannaSongChanged;
        public event EventHandler<ValueChangedEventArgs<Song[]>> SongsChanged;

        public Guid ID { get; }

        public string Name
        {
            get => name;
            set
            {
                if (value == name) return;

                var args = new ValueChangedEventArgs<string>(Name, value);
                name = value;
                NameChanged?.Invoke(this, args);

                OnNameChanged();
            }
        }

        public OrderType Shuffle
        {
            get => shuffle;
            set
            {
                if (value == shuffle) return;

                var args = new ValueChangedEventArgs<OrderType>(Shuffle, value);
                shuffle = value;
                ShuffleChanged?.Invoke(this, args);

                OnShuffleChanged();
            }
        }

        public LoopType Loop
        {
            get => loop;
            set
            {
                if (value == loop) return;

                var args = new ValueChangedEventArgs<LoopType>(Loop, value);
                loop = value;
                LoopChanged?.Invoke(this, args);

                OnLoopChanged();
            }
        }

        public TimeSpan Position
        {
            get => position;
            set
            {
                value = TimeSpan.FromSeconds(Math.Round(value.TotalSeconds));

                if (value == position) return;

                var args = new ValueChangedEventArgs<TimeSpan>(Position, value);
                position = value;
                PositionChanged?.Invoke(this, args);

                OnPositionChanged();
            }
        }

        public TimeSpan Duration
        {
            get => duration;
            set
            {
                if (value == duration) return;

                var args = new ValueChangedEventArgs<TimeSpan>(Duration, value);
                duration = value;
                DurationChanged?.Invoke(this, args);

                OnDurationChanged();
            }
        }

        public Song? CurrentSong
        {
            get => currentSong;
            set
            {
                if (value == currentSong) return;

                var args = new ValueChangedEventArgs<Song?>(CurrentSong, value);
                currentSong = value;
                CurrentSongChanged?.Invoke(this, args);

                OnCurrentSongChanged();
            }
        }

        public RequestSong? WannaSong
        {
            get => wannaSong;
            set
            {
                if (value.HasValue == wannaSong.HasValue && value.Equals(wannaSong)) return;

                var args = new ValueChangedEventArgs<RequestSong?>(WannaSong, value);
                wannaSong = value;
                WannaSongChanged?.Invoke(this, args);

                OnPropertyChanged(nameof(WannaSong));
            }
        }

        public Song[] Songs
        {
            get => songs;
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));

                if (value == songs) return;

                for (int i = 0; i < value.Length; i++) value[i].Index = i;

                var args = new ValueChangedEventArgs<Song[]>(Songs, value);
                songs = value;
                SongsChanged?.Invoke(this, args);

                OnSongsChanged();
            }
        }

        public IEnumerable<Song> AllSongs
        {
            get => allSongs;
            private set
            {
                if (ReferenceEquals(value, allSongs)) return;

                allSongs = value;
                OnAllSongsChanged();
            }
        }

        public Playlist(IInvokeDispatcherHelper helper = null) : this(Guid.NewGuid(), helper)
        {
        }

        public Playlist(Guid id, IInvokeDispatcherHelper helper = null)
        {
            this.helper = helper;

            ID = id;
            Loop = LoopType.CurrentPlaylist;
            WannaSong = null;
            Songs = new Song[0];
        }

        protected virtual void OnShuffleChanged()
        {
            OnPropertyChanged(nameof(Shuffle));

            AllSongs = SongsHelper.GetAllSongs(this).ToBuffer();
        }

        protected virtual void OnNameChanged()
        {
            OnPropertyChanged(nameof(Name));
        }

        protected virtual void OnLoopChanged()
        {
            OnPropertyChanged(nameof(Loop));
        }

        protected virtual void OnPositionChanged()
        {
            OnPropertyChanged(nameof(Position));
        }

        protected virtual void OnDurationChanged()
        {
            OnPropertyChanged(nameof(Duration));
        }

        protected virtual void OnCurrentSongChanged()
        {
            OnPropertyChanged(nameof(CurrentSong));
        }

        protected virtual void OnSongsChanged()
        {
            OnPropertyChanged(nameof(Songs));

            AllSongs = SongsHelper.GetAllSongs(this).ToBuffer();
        }

        protected virtual void OnAllSongsChanged()
        {
            OnPropertyChanged(nameof(AllSongs));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged == null) return;

            if (helper != null) helper.InvokeDispatcher(Raise);
            else Raise();

            void Raise() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Playlist);
        }

        public bool Equals(Playlist other)
        {
            return other != null &&
                   ID.Equals(other.ID) &&
                   Shuffle == other.Shuffle &&
                   Loop == other.Loop &&
                   Position.Equals(other.Position) &&
                   Duration.Equals(other.Duration) &&
                   EqualityComparer<Song?>.Default.Equals(CurrentSong, other.CurrentSong) &&
                   EqualityComparer<Song[]>.Default.Equals(Songs, other.Songs);
        }

        public static bool operator ==(Playlist playlist1, IPlaylistBase playlist2)
        {
            return EqualityComparer<IPlaylistBase>.Default.Equals(playlist1, playlist2);
        }

        public static bool operator !=(Playlist playlist1, IPlaylistBase playlist2)
        {
            return !(playlist1 == playlist2);
        }
    }
}
