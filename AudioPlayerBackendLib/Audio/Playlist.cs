using StdOttStandard;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace AudioPlayerBackend.Audio
{
    public class Playlist : IPlaylist, IEquatable<Playlist>
    {
        private static readonly Dictionary<Guid, Playlist> playlists = new Dictionary<Guid, Playlist>();

        public static bool TryGetInstance(Guid id,out Playlist playlist)
        {
            lock (playlists)
            {
                return playlists.TryGetValue(id, out playlist);
            }
        }

        public static Playlist GetInstance(Guid id, INotifyPropertyChangedHelper helper = null)
        {
            lock (playlists)
            {
                Playlist playlist;

                if (playlists.TryGetValue(id, out playlist)) return playlist;

                return new Playlist(id, helper);
            }
        }

        public static Playlist GetNew(INotifyPropertyChangedHelper helper = null)
        {
            lock (playlists)
            {
                Guid id;
                do
                {
                    id = Guid.NewGuid();
                }
                while (playlists.ContainsKey(id));

                return new Playlist(id, helper);
            }
        }

        private bool isAllShuffle;
        private TimeSpan position, duration;
        private LoopType loop;
        private Song? currentSong;
        private Song[] songs;
        private IEnumerable<Song> allSongs;
        private INotifyPropertyChangedHelper helper;

        public event EventHandler<ValueChangedEventArgs<bool>> IsAllShuffleChanged;
        public event EventHandler<ValueChangedEventArgs<LoopType>> LoopChanged;
        public event EventHandler<ValueChangedEventArgs<TimeSpan>> PositionChanged;
        public event EventHandler<ValueChangedEventArgs<TimeSpan>> DurationChanged;
        public event EventHandler<ValueChangedEventArgs<Song?>> CurrentSongChanged;
        public event EventHandler<ValueChangedEventArgs<Song[]>> SongsChanged;

        public Guid ID { get; }

        public bool IsAllShuffle
        {
            get => isAllShuffle;
            set
            {
                if (value == isAllShuffle) return;

                var args = new ValueChangedEventArgs<bool>(IsAllShuffle, value);
                isAllShuffle = value;
                IsAllShuffleChanged?.Invoke(this, args);

                OnIsAllShuffleChanged();
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

        public Song[] Songs
        {
            get => songs;
            set
            {
                if (value.BothNullOrSequenceEqual(songs)) return;

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

        protected Playlist(Guid id, INotifyPropertyChangedHelper helper = null)
        {
            this.helper = helper;

            lock (playlists) playlists.Add(id, this);

            ID = id;
            Loop = LoopType.CurrentPlaylist;
            Songs = new Song[0];
        }

        public void SetNextSong()
        {
            ChangeCurrentSongOrRestart(SongsService.GetNextSong(this).song);
        }

        public void SetPreviousSong()
        {
            ChangeCurrentSongOrRestart(SongsService.GetPreviousSong(this).song);
        }

        private void ChangeCurrentSongOrRestart(Song? newCurrentSong)
        {
            if (newCurrentSong != CurrentSong) CurrentSong = newCurrentSong;
            else Position = TimeSpan.Zero;
        }

        protected virtual void OnIsAllShuffleChanged()
        {
            OnPropertyChanged(nameof(IsAllShuffle));

            AllSongs = SongsService.GetAllSongs(this).ToBuffer();
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

            AllSongs = SongsService.GetAllSongs(this).ToBuffer();
        }

        protected virtual void OnAllSongsChanged()
        {
            OnPropertyChanged(nameof(AllSongs));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged == null) return;

            if (helper?.InvokeDispatcher != null) helper.InvokeDispatcher(Raise);
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
                   IsAllShuffle == other.IsAllShuffle &&
                   Loop == other.Loop &&
                   Position.Equals(other.Position) &&
                   Duration.Equals(other.Duration) &&
                   EqualityComparer<Song?>.Default.Equals(CurrentSong, other.CurrentSong) &&
                   EqualityComparer<Song[]>.Default.Equals(Songs, other.Songs);
        }

        public override int GetHashCode()
        {
            var hashCode = -1043069822;
            hashCode = hashCode * -1521134295 + EqualityComparer<IEnumerable<Song>>.Default.GetHashCode(AllSongs);
            hashCode = hashCode * -1521134295 + EqualityComparer<Guid>.Default.GetHashCode(ID);
            hashCode = hashCode * -1521134295 + IsAllShuffle.GetHashCode();
            hashCode = hashCode * -1521134295 + Loop.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<TimeSpan>.Default.GetHashCode(Position);
            hashCode = hashCode * -1521134295 + EqualityComparer<TimeSpan>.Default.GetHashCode(Duration);
            hashCode = hashCode * -1521134295 + EqualityComparer<Song?>.Default.GetHashCode(CurrentSong);
            hashCode = hashCode * -1521134295 + EqualityComparer<Song[]>.Default.GetHashCode(Songs);
            return hashCode;
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
