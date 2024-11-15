using System;
using System.Text;
using AudioPlayerBackend.ViewModels;
using AudioPlayerBackend.Player;
using AudioPlayerBackend.AudioLibrary;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using System.Threading.Tasks;
using AudioPlayerBackend.FileSystem;
using StdOttStandard.Linq.DataStructures;
using System.Collections.Generic;

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
        private const string logFileName = "test.log";

        private static readonly LockQueue<string> queue = new LockQueue<string>();
        private static readonly StringBuilder builder = new StringBuilder();
        private static readonly object writerRunningLockObj = new object();
        private static bool isWriterRunning = false;
        private static IFileSystemService fileSystemService;

        public static void SetFileSystemService(IFileSystemService service)
        {
            fileSystemService = service;
        }

        public static async void Log(string text, params object[] values)
        {
            string line = GetLogLine($"{text}: {string.Join(" | ", values)}");
            System.Diagnostics.Debug.WriteLine(line);

            queue.Enqueue(line);
            StartWriter();
        }

        private static string GetLogLine(string text)
        {
            DateTime n = DateTime.Now;
            return $"{n.Day:00}.{n.Month:00}.{n.Year} {n.Hour:00}:{n.Minute:00}:{n.Second:00}.{n.Millisecond:000}: {text}";
        }

        private static async void StartWriter()
        {
            lock (writerRunningLockObj)
            {
                if (isWriterRunning) return;

                isWriterRunning = true;
            }

            await Task.Run(async () =>
            {
                List<string> newLines = new List<string>();
                while (true)
                {
                    do
                    {
                        if (!queue.TryDequeue(out string line)) return;

                        newLines.Add(line);
                        builder.AppendLine(line);
                    }
                    while (queue.Count > 0);

                    if (fileSystemService != null) await fileSystemService?.AppendTextLines(logFileName, newLines);

                    newLines.Clear();
                }
            });
        }

        public static void Clear()
        {
            builder.Clear();
        }

        public static string Get()
        {
            return builder.ToString();
        }

        public static Task ClearAll()
        {
            builder.Clear();
            return fileSystemService.WriteTextFile(logFileName, string.Empty);
        }

        public static Task<string> GetFile()
        {
            return fileSystemService.ReadTextFile(logFileName);
        }
    }
}
