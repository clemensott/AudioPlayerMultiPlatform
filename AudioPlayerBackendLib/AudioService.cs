using System;
using System.Windows;
using System.Windows.Controls;

namespace AudioPlayerBackendLib
{
    public class AudioService : IAudioService
    {
        private static Instance instance;

        private static Instance Instance
        {
            get
            {
                if (instance == null) instance = new Instance(false);

                return instance;
            }
        }

        public void CloseInstance()
        {
            instance?.Dispose();
            instance = null;
        }

        public Song[] GetAllSongs()
        {
            return Instance.AllSongs;
        }

        public Song GetCurrentSong()
        {
            return Instance.CurrentSong;
        }

        public string GetDebugInfo()
        {
            throw new NotImplementedException();
        }

        public TimeSpan GetDuration()
        {
            Duration duration = Instance.Player.NaturalDuration;
            return duration.HasTimeSpan ? duration.TimeSpan : TimeSpan.Zero;
        }

        public bool GetIsAllShuffle()
        {
            return Instance.IsAllShuffle;
        }

        public bool GetIsOnlySearch()
        {
            return Instance.IsOnlySearch;
        }

        public bool GetIsSearchShuffle()
        {
            return Instance.IsSearchShuffle;
        }

        public MediaElement GetMediaElement()
        {
            if (instance == null) instance = new Instance(false);

            return instance.Player;
        }

        public string[] GetMediaSources()
        {
            return Instance.MediaSources;
        }

        public PlayState GetPlayState()
        {
            return Instance.Player.LoadedBehavior.ToPlayState();
        }

        public TimeSpan GetPositon()
        {
            return Instance.Player.Position;
        }

        public string GetSearchKey()
        {
            return Instance.SearchKey;
        }

        public Song[] GetSearchSongs()
        {
            return Instance.SearchSongs;
        }

        public States GetStates()
        {
            return new States(GetPositon(), GetDuration(), GetPlayState(), GetIsAllShuffle(), GetIsSearchShuffle(),
                GetIsOnlySearch(), GetMediaSources(), GetCurrentSong(), GetAllSongs(), GetSearchSongs());
        }

        public void Next()
        {
            Instance.SetNextSong();
        }

        public void Pause()
        {
            Instance.Player.LoadedBehavior = PlayState.Pause.ToMediaState();
        }

        public void Play()
        {
            Instance.Player.LoadedBehavior = PlayState.Play.ToMediaState();
        }

        public void Previous()
        {
            Instance.SetPreviousSong();
        }

        public void Refresh()
        {
            Instance.Refresh();
        }

        public void SetCurrentSong(Song song)
        {
            Instance.CurrentSong = song;
        }

        public void SetIsAllShuffle(bool isAllShuffle)
        {
            Instance.IsAllShuffle = isAllShuffle;
        }

        public void SetIsOnlySearch(bool isOnlySearch)
        {
            Instance.IsOnlySearch = isOnlySearch;
        }

        public void SetIsSearchShuffle(bool isSearchShuffle)
        {
            Instance.IsSearchShuffle = isSearchShuffle;
        }

        public void SetMediaSources(string[] sources)
        {
            Instance.MediaSources = sources;
        }

        public void SetPlayState(PlayState state)
        {
            Instance.Player.LoadedBehavior = state.ToMediaState();
        }

        public void SetPosition(TimeSpan position)
        {
            Instance.Player.Position = position;
        }

        public void SetSearchKey(string searchKey)
        {
            Instance.SearchKey = searchKey;
        }

        public void Stop()
        {
            Instance.Player.LoadedBehavior = PlayState.Stop.ToMediaState();
        }
    }
}
