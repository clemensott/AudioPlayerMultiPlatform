using System;
using StdOttStandard.Linq;
using System.Collections.Generic;
using System.Linq;
using AudioPlayerBackend.Audio;
using System.Text;

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
            IEnumerable<Song> songs, IInvokeDispatcherHelper helper)
        {
            AddSongsToFirstPlaylist(service, songs, false, helper);
        }

        public static void AddSongsToFirstPlaylist(this IAudioService service, IEnumerable<Song> songs,
            bool prepend, IInvokeDispatcherHelper helper)
        {
            songs = songs as Song[] ?? songs.ToArray();

            if (!songs.Any()) return;

            IPlaylist currentPlaylist = service.CurrentPlaylist;
            Song? currentSong = currentPlaylist?.CurrentSong;

            if (service.Playlists.Count > 0)
            {
                IPlaylist playlist = service.Playlists[0];

                if (playlist.ID == currentPlaylist?.ID)
                {
                    if (prepend)
                    {
                        playlist.Songs = songs.Concat(playlist.Songs).Distinct().ToArray();
                        playlist.WannaSong = RequestSong.Start(songs.First());
                    }
                    else playlist.Songs = playlist.Songs.Concat(songs).Distinct().ToArray();
                }
                else
                {
                    if (prepend || !currentSong.HasValue)
                    {
                        playlist.Songs = songs.Distinct().ToArray();
                        playlist.WannaSong = RequestSong.Start(songs.First());
                        playlist.Duration = currentPlaylist.Duration;
                        playlist.Position = currentPlaylist.Position;

                        service.CurrentPlaylist = playlist;
                    }
                    else
                    {
                        playlist.Songs = songs.Insert(0, currentSong.Value).Distinct().ToArray();
                        playlist.WannaSong = RequestSong.Get(currentSong.Value, null, currentPlaylist.Duration);

                        service.CurrentPlaylist = playlist;

                        currentPlaylist.CurrentSong = currentPlaylist.Songs.Cast<Song?>()
                            .NextOrDefault(currentSong).next;
                        currentPlaylist.Position = TimeSpan.Zero;
                        currentPlaylist.WannaSong = RequestSong.Start(currentPlaylist.CurrentSong);
                    }
                }
            }
            else
            {
                IPlaylist playlist = new Playlist(helper)
                {
                    Name = "Custom",
                    Loop = LoopType.Next,
                    Shuffle = OrderType.Custom
                };

                if (prepend || !currentSong.HasValue)
                {
                    playlist.Songs = songs.ToArray();
                    playlist.WannaSong = RequestSong.Start(songs.First());
                    playlist.Duration = currentPlaylist.Duration;
                    playlist.Position = currentPlaylist.Position;

                    service.Playlists.Add(playlist);
                    service.CurrentPlaylist = playlist;
                }
                else
                {
                    playlist.Songs = songs.Insert(0, currentSong.Value).ToArray();
                    playlist.WannaSong = RequestSong.Get(currentSong.Value, null, currentPlaylist.Duration);

                    service.Playlists.Add(playlist);
                    service.CurrentPlaylist = playlist;

                    currentPlaylist.CurrentSong = currentPlaylist.Songs.Cast<Song?>()
                        .NextOrDefault(currentSong).next;
                    currentPlaylist.Position = TimeSpan.Zero;
                    currentPlaylist.WannaSong = RequestSong.Start(currentPlaylist.CurrentSong);
                }
            }
        }
    }

    public static class Logs
    {
        private static readonly StringBuilder builder = new StringBuilder();

        public static void Log(string text, params object[] values)
        {
            string line = GetLogLine($"{text}: {string.Join(" | ",values)}");
            System.IO.File.AppendAllLines("./test.log", new string[] { line });
            builder.AppendLine(line);
        }

        private static string GetLogLine(string text)
        {
            DateTime n = DateTime.Now;
            return $"{n.Day:00}.{n.Month:00}.{n.Year} {n.Hour:00}:{n.Minute:00}:{n.Second:00}.{n.Millisecond:000}: {text}";
        }

        public static void Clear()
        {
            builder.Clear();
        }

        public static string Get()
        {
            return builder.ToString();
        }
    }
}
