using System;
using StdOttStandard.Linq;
using System.Collections.Generic;
using System.Linq;
using AudioPlayerBackend.Audio;

namespace AudioPlayerBackend
{
    public static class FrontendUtils
    {
        public static void AddSongsToFirstPlaylist(this IAudioService service, IEnumerable<Song> songs)
        {
            AddSongsToFirstPlaylist(service, songs, false, null);
        }

        public static void AddSongsToFirstPlaylist(this IAudioService service, IEnumerable<Song> songs, bool prepend)
        {
            AddSongsToFirstPlaylist(service, songs, prepend, null);
        }

        public static void AddSongsToFirstPlaylist(this IAudioService service,
            IEnumerable<Song> songs, INotifyPropertyChangedHelper helper)
        {
            AddSongsToFirstPlaylist(service, songs, false, helper);
        }

        public static void AddSongsToFirstPlaylist(this IAudioService service, IEnumerable<Song> songs,
            bool prepend, INotifyPropertyChangedHelper helper)
        {
            songs = songs as Song[] ?? songs.ToArray();

            if (!songs.Any()) return;

            IPlaylist currentPlaylist = service.CurrentPlaylist;

            if (service.Playlists.Length > 0)
            {
                IPlaylist playlist = service.Playlists[0];

                if (playlist.ID == currentPlaylist.ID)
                {
                    if (prepend)
                    {
                        playlist.Songs = songs.Concat(playlist.Songs).Distinct().ToArray();
                        playlist.WannaSong = RequestSong.Get(songs.First());
                    }
                    else playlist.Songs = playlist.Songs.Concat(songs).Distinct().ToArray();
                }
                else
                {
                    if (prepend || !currentPlaylist.CurrentSong.HasValue)
                    {
                        playlist.Songs = songs.ToArray();
                        playlist.WannaSong = RequestSong.Get(songs.First());

                        service.CurrentPlaylist = playlist;
                    }
                    else
                    {
                        playlist.Songs = songs.Insert(0, currentPlaylist.CurrentSong.Value).ToArray();
                        playlist.WannaSong = RequestSong.Get(currentPlaylist.CurrentSong.Value,
                            currentPlaylist.Position, currentPlaylist.Duration);

                        service.CurrentPlaylist = playlist;

                        currentPlaylist.CurrentSong = currentPlaylist.Songs.Cast<Song?>()
                            .NextOrDefault(currentPlaylist.CurrentSong).next;
                        currentPlaylist.Position = TimeSpan.Zero;
                        currentPlaylist.WannaSong = RequestSong.Get(currentPlaylist.CurrentSong);
                    }
                }
            }
            else
            {
                IPlaylist playlist = new Playlist(helper);
                playlist.Loop = LoopType.Next;
                playlist.IsAllShuffle = true;

                if (prepend || !currentPlaylist.CurrentSong.HasValue)
                {
                    playlist.Songs = songs.ToArray();
                    playlist.WannaSong = RequestSong.Get(songs.First());

                    service.Playlists = service.Playlists.ConcatParams(playlist).Distinct().ToArray();
                    service.CurrentPlaylist = playlist;
                }
                else
                {
                    playlist.Songs = songs.Insert(0, currentPlaylist.CurrentSong.Value).ToArray();
                    playlist.WannaSong = RequestSong.Get(currentPlaylist.CurrentSong.Value,
                        currentPlaylist.Position, currentPlaylist.Duration);

                    service.Playlists = service.Playlists.ConcatParams(playlist).Distinct().ToArray();
                    service.CurrentPlaylist = playlist;

                    currentPlaylist.CurrentSong = currentPlaylist.Songs.Cast<Song?>()
                        .NextOrDefault(currentPlaylist.CurrentSong).next;
                    currentPlaylist.Position = TimeSpan.Zero;
                    currentPlaylist.WannaSong = RequestSong.Get(currentPlaylist.CurrentSong);
                }
            }
        }
    }
}
