using AudioPlayerBackendLib;
using System;
using System.Linq;
using System.Windows.Controls;

namespace AudioPlayerFrontendWpf.Independent
{
    class WcfClient : AudioServiceReference.AudioServiceClient, IAudioService
    {
        public new Song[] GetAllSongs()
        {
            return base.GetAllSongs().Select(Convert).ToArray();
        }

        public new Song GetCurrentSong()
        {
            return Convert(base.GetCurrentSong());
        }

        public MediaElement GetMediaElement()
        {
            throw new NotImplementedException();
        }

        public new PlayState GetPlayState()
        {
            return Convert(base.GetPlayState());
        }

        public new Song[] GetSearchSongs()
        {
            return base.GetSearchSongs().Select(Convert).ToArray();
        }

        public new States GetStates()
        {
            return Convert(base.GetStates());
        }

        public void SetCurrentSong(Song song)
        {
            SetCurrentSong(Convert(song));
        }

        public void SetPlayState(PlayState state)
        {
            SetPlayState(Convert(state));
        }

        private static Song Convert(AudioServiceReference.Song song)
        {
            return new Song(song.Index, song.Title, song.Artist, song.FullPath);
        }

        private static AudioServiceReference.Song Convert(Song song)
        {
            return new AudioServiceReference.Song()
            {
                Index = song.Index,
                Title = song.Title,
                Artist = song.Artist,
                FullPath = song.FullPath
            };
        }

        private static States Convert(AudioServiceReference.States s)
        {
            PlayState playState = Convert(s.PlayState);
            Hashes hashes = new Hashes(s.Hashes.MediaSourcesHash, s.Hashes.CurrentSongHash, s.Hashes.AllSongsHash, s.Hashes.SearchSongsHash);

            return new States(s.Position, s.Duration, playState, s.IsAllShuffle, s.IsSearchShuffle, s.IsOnlySearch, s.SearchKey, hashes);
        }

        private static PlayState Convert(string state)
        {
            return (PlayState)Enum.Parse(typeof(PlayState), state);
        }

        private static string Convert(PlayState state)
        {
            return Enum.GetName(typeof(PlayState), state);
        }
    }
}
