using System;
using StdOttStandard;
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

            if (service.Playlists.Length > 0)
            {
                IPlaylist playlist = service.Playlists[0];

                if (playlist.ID == service.CurrentPlaylist.ID)
                {
                    if (prepend) playlist.Songs = songs.Concat(playlist.Songs).Distinct().ToArray();
                    else playlist.Songs = playlist.Songs.Concat(songs).Distinct().ToArray();
                }
                else
                {
                    playlist.Songs = songs.ToArray();
                    playlist.Position = TimeSpan.Zero;
                }

                service.CurrentPlaylist = playlist;
            }
            else
            {
                IPlaylist newPlaylist = new Playlist(helper);
                newPlaylist.Loop = LoopType.Next;
                newPlaylist.IsAllShuffle = true;
                newPlaylist.Songs = songs.ToArray();
                newPlaylist.CurrentSong = songs.First();

                service.Playlists = service.Playlists.Concat(newPlaylist).Distinct().ToArray();
                service.CurrentPlaylist = newPlaylist;
            }
        }
    }
}
