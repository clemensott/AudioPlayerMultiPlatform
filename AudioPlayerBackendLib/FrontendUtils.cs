using System;
using System.Text;
using AudioPlayerBackend.ViewModels;
using AudioPlayerBackend.Player;
using AudioPlayerBackend.AudioLibrary;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using System.Threading.Tasks;

namespace AudioPlayerBackend
{
    public static class FrontendUtils
    {
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

        public static async Task SetRestartCurrentSong(this IPlaylistViewModel viewModel)
        {
            await viewModel.SetCurrentSongRequest(SongRequest.Start(viewModel.CurrentSongRequest?.Id));
        }

        public static async Task SetNextSong(this IPlaylistViewModel viewModel)
        {
            Song? newCurrentSong = SongsHelper.GetNextSong(viewModel.Songs, viewModel.Shuffle, viewModel.CurrentSong).song;
            await viewModel.SetCurrentSongRequest(SongRequest.Start(newCurrentSong?.Id));
        }

        public static async Task SetPreviousSong(this IPlaylistViewModel viewModel)
        {
            Song? newCurrentSong = SongsHelper.GetPreviousSong(viewModel.Songs, viewModel.Shuffle, viewModel.CurrentSong).song;
            await viewModel.SetCurrentSongRequest(SongRequest.Start(newCurrentSong?.Id));
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
                builder.AppendLine(line);
                System.Diagnostics.Debug.WriteLine(line);
                //System.IO.File.AppendAllLines("./test.log", new string[] { line });
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
