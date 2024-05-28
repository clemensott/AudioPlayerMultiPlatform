using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AudioPlayerBackend.Audio;
using AudioPlayerBackend.FileSystem;
using Newtonsoft.Json;
using StdOttStandard.Linq;

namespace AudioPlayerBackend.Data
{
    public class ReadWriteAudioServiceData : IDisposable
    {
        private readonly IAudioCreateService audioCreateService;
        private readonly IFileSystemService fileSystemService;
        private readonly string path;
        private AudioServiceData preloadData;
        private SemaphoreSlim saveSem;
        private Task saveHandlerTask;
        private bool disposed;

        public IAudioServiceBase Service { get; private set; }

        private ReadWriteAudioServiceData(string path)
        {
            audioCreateService = AudioPlayerServiceProvider.Current.GetAudioCreateService();
            fileSystemService = AudioPlayerServiceProvider.Current.GetFileSystemService();
            this.path = path;
        }

        public static async Task<ReadWriteAudioServiceData> Preload(string path)
        {
            ReadWriteAudioServiceData dataService = new ReadWriteAudioServiceData(path);
            if (!string.IsNullOrWhiteSpace(path))
            {
                await Task.Run(dataService.PreloadData);

                dataService.saveSem = new SemaphoreSlim(0);
                dataService.saveHandlerTask = Task.Run(dataService.SaveHandler);
            }

            return dataService;
        }

        private async Task PreloadData()
        {
            try
            {
                string jsonText = await fileSystemService.ReadTextFile(path);
                preloadData = JsonConvert.DeserializeObject<AudioServiceData>(jsonText);
            }
            catch { }
        }

        public void Init(IAudioService service)
        {
            Service = service;

            if (!string.IsNullOrWhiteSpace(path))
            {
                Load();
                Subscribe();
            }
        }

        private void Subscribe()
        {
            if (Service == null) return;

            Service.CurrentPlaylistChanged += Save;
            Service.SourcePlaylistsChanged += Service_SourcePlaylistsChanged;
            Service.PlaylistsChanged += Service_PlaylistsChanged;
            Service.VolumeChanged += Save;

            Subscribe(Service.CurrentPlaylist);
            Service.SourcePlaylists.ForEach(Subscribe);
            Service.Playlists.ForEach(Subscribe);
        }

        private void Unsubscribe()
        {
            if (Service == null) return;

            Service.CurrentPlaylistChanged -= Save;
            Service.SourcePlaylistsChanged -= Service_SourcePlaylistsChanged;
            Service.PlaylistsChanged -= Service_PlaylistsChanged;
            Service.VolumeChanged -= Save;

            Unsubscribe(Service.CurrentPlaylist);
            Service.SourcePlaylists.ForEach(Unsubscribe);
            Service.Playlists.ForEach(Unsubscribe);
        }

        private void Subscribe(ISourcePlaylistBase playlist)
        {
            if (playlist == null) return;

            Subscribe((IPlaylistBase)playlist);
            playlist.FileMediaSourcesChanged += Save;
        }

        private void Unsubscribe(ISourcePlaylistBase playlist)
        {
            if (playlist == null) return;

            Unsubscribe((IPlaylistBase)playlist);
            playlist.FileMediaSourcesChanged -= Save;
        }

        private void Subscribe(IPlaylistBase playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged += Save;
            playlist.ShuffleChanged += Save;
            playlist.LoopChanged += Save;
            playlist.DurationChanged += Save;
            playlist.SongsChanged += Save;
        }

        private void Unsubscribe(IPlaylistBase playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged -= Save;
            playlist.ShuffleChanged -= Save;
            playlist.LoopChanged -= Save;
            playlist.DurationChanged -= Save;
            playlist.SongsChanged -= Save;
        }

        private void Save(object sender, EventArgs args)
        {
            TriggerSave();
        }

        private void Service_SourcePlaylistsChanged(object sender, ValueChangedEventArgs<ISourcePlaylistBase[]> e)
        {
            e.OldValue.ForEach(Unsubscribe);
            e.NewValue.ForEach(Subscribe);

            TriggerSave();
        }

        private void Service_PlaylistsChanged(object sender, ValueChangedEventArgs<IPlaylistBase[]> e)
        {
            e.OldValue.ForEach(Unsubscribe);
            e.NewValue.ForEach(Subscribe);

            TriggerSave();
        }

        private void Load()
        {
            if (preloadData == null) return;

            Service.Volume = preloadData.Volume;
            Service.FileMediaSourceRoots = preloadData.FileMediaSourceRoots;

            IDictionary<string, Song> allSongs = new Dictionary<string, Song>();

            Service.SourcePlaylists = preloadData.SourcePlaylists.Select(playlistData =>
            {
                Guid id = Guid.Parse(playlistData.ID);
                ISourcePlaylistBase playlist = Service.SourcePlaylists
                    .FirstOrDefault(s => s.ID == id) ?? audioCreateService.CreateSourcePlaylist(id);

                MergePlaylist(playlist, playlistData);

                foreach (Song song in playlist.Songs) allSongs[song.FullPath] = song;

                return playlist;
            }).ToArray();

            Service.Playlists = preloadData.Playlists.Select(playlistData =>
            {
                Guid id = Guid.Parse(playlistData.ID);
                IPlaylistBase playlist = Service.Playlists
                    .FirstOrDefault(s => s.ID == id) ?? audioCreateService.CreatePlaylist(id);

                playlistData.Songs = playlistData.Songs.Where(s => allSongs.ContainsKey(s.FullPath)).ToArray();

                MergePlaylist(playlist, playlistData);

                return playlist;
            }).ToArray();

            if (string.IsNullOrWhiteSpace(preloadData.CurrentPlaylistID)) Service.CurrentPlaylist = null;
            else
            {
                Guid currentPlaylistID = Guid.Parse(preloadData.CurrentPlaylistID);
                Service.CurrentPlaylist = Service.GetAllPlaylists().FirstOrDefault(p => p.ID == currentPlaylistID);
            }
        }

        private static void MergePlaylist(ISourcePlaylistBase playlist, SourcePlaylistData data)
        {
            playlist.FileMediaSources = data.Sources;
            MergePlaylist((IPlaylistBase)playlist, data);
        }

        private static void MergePlaylist(IPlaylistBase playlist, PlaylistData data)
        {
            playlist.Shuffle = data.Shuffle;
            playlist.Loop = data.Loop;
            playlist.Name = data.Name;
            playlist.Songs = data.Songs;

            Song currentSong;
            if (playlist.Songs.TryFirst(s => s.FullPath == data.CurrentSongPath, out currentSong))
            {
                playlist.CurrentSong = currentSong;
                playlist.Position = data.Position;
                playlist.Duration = data.Duration;
                playlist.WannaSong = RequestSong.Get(currentSong, data.Position, data.Duration);
            }
        }

        private void TriggerSave()
        {
            saveSem?.Release();
        }

        private async Task SaveHandler()
        {
            while (!disposed)
            {
                try
                {
                    do
                    {
                        await saveSem.WaitAsync();
                    }
                    while (saveSem.CurrentCount > 0);

                    await Task.Delay(400);
                    await Save();
                    await Task.Delay(500);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e);
                }
            }
        }

        private async Task Save()
        {
            AudioServiceData data = new AudioServiceData(Service);
            string jsonText = JsonConvert.SerializeObject(data);
            await fileSystemService.WriteTextFile(path, jsonText);
        }

        public void Dispose()
        {
            Unsubscribe();
            disposed = true;
            saveSem?.Release();
            saveHandlerTask?.Wait();
        }
    }
}
