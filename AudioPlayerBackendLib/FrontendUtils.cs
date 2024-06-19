using System;
using StdOttStandard.Linq;
using System.Collections.Generic;
using System.Linq;
using AudioPlayerBackend.Audio;
using System.Text;
using AudioPlayerBackend.ViewModels;
using AudioPlayerBackend.Player;

namespace AudioPlayerBackend
{
    public static class FrontendUtils
    {
        public static void AddSongsToFirstPlaylist(this IAudioService service, IEnumerable<Song> songs)
        {
            AddSongsToFirstPlaylist(service, songs, false);
        }

        public static void AddSongsToFirstPlaylist(this IAudioService service, IEnumerable<Song> songs, bool prepend)
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
                IPlaylist playlist = AudioPlayerServiceProvider.Current.GetAudioCreateService().CreatePlaylist(Guid.NewGuid());
                playlist.Name = "Custom";
                playlist.Loop = LoopType.Next;
                playlist.Shuffle = OrderType.Custom;

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

        public static void SetTogglePlayState(this ILibraryViewModel viewModel)
        {
            if (viewModel != null)
            {
                viewModel.PlayState = viewModel.PlayState == PlaybackState.Playing ?
                    PlaybackState.Paused : PlaybackState.Playing;
            }
        }

        public static void SetPlay(this ILibraryViewModel viewModel)
        {
            if (viewModel != null) viewModel.PlayState = PlaybackState.Playing;
        }

        public static void SetPause(this ILibraryViewModel viewModel)
        {
            if (viewModel != null) viewModel.PlayState = PlaybackState.Paused;
        }

        public static void SetRestartCurrentSong(this IPlaylistViewModel viewModel)
        {
            if (viewModel != null) viewModel.Position = TimeSpan.Zero;
        }

        public static void SetNextSong(this IPlaylistViewModel viewModel)
        {
        }

        public static void SetPreviousSong(this IPlaylistViewModel viewModel)
        {
        }
    }

    public static class Logs
    {
        private static readonly StringBuilder builder = new StringBuilder();

        public static void Log(string text, params object[] values)
        {
            try
            {
                string line = GetLogLine($"{text}: {string.Join(" | ", values)}");
                System.IO.File.AppendAllLines("./test.log", new string[] { line });
                builder.AppendLine(line);
            }
            catch { }
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
