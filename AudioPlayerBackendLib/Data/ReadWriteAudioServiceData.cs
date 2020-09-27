using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AudioPlayerBackend.Audio;
using StdOttStandard;
using StdOttStandard.Linq;

namespace AudioPlayerBackend.Data
{
    public class ReadWriteAudioServiceData : IDisposable
    {
        private static readonly Random rnd = new Random();

        private readonly INotifyPropertyChangedHelper helper;
        private readonly string path;
        private SemaphoreSlim saveSem;
        private bool disposed;

        public IAudioServiceBase Service { get; }

        private ReadWriteAudioServiceData(string path, IAudioServiceBase service, INotifyPropertyChangedHelper helper)
        {
            this.path = path;
            Service = service;
            this.helper = helper;
        }

        public static ReadWriteAudioServiceData Start(string path, IAudioServiceBase service, INotifyPropertyChangedHelper helper = null)
        {
            ReadWriteAudioServiceData dataService = new ReadWriteAudioServiceData(path, service, helper);
            dataService.Init();
            return dataService;
        }

        private void Init()
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                Load();

                saveSem = new SemaphoreSlim(0);
                Task.Run(SaveHandler);
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
            playlist.IsAllShuffleChanged += Save;
            playlist.LoopChanged += Save;
            playlist.DurationChanged += Save;
            playlist.SongsChanged += Save;
        }

        private void Unsubscribe(IPlaylistBase playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged -= Save;
            playlist.IsAllShuffleChanged -= Save;
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
            AudioServiceData data;
            if (!File.Exists(path)) return;

            try
            {
                data = StdUtils.XmlDeserializeFile<AudioServiceData>(path);
            }
            catch
            {
                return;
            }

            Service.Volume = data.Volume;

            IDictionary<string, Song> allSongs = new Dictionary<string, Song>();

            Service.SourcePlaylists = data.SourcePlaylists.Select(playlistData =>
            {
                Guid id = Guid.Parse(playlistData.ID);
                ISourcePlaylistBase playlist = Service.SourcePlaylists
                    .FirstOrDefault(s => s.ID == id) ?? new SourcePlaylist(id, helper);

                MergePlaylist(playlist, playlistData);

                foreach (Song song in playlist.Songs) allSongs[song.FullPath] = song;

                return playlist;
            }).ToArray();

            Service.Playlists = data.Playlists.Select(playlistData =>
            {
                Guid id = Guid.Parse(playlistData.ID);
                IPlaylistBase playlist = Service.Playlists
                    .FirstOrDefault(s => s.ID == id) ?? new Playlist(id, helper);

                playlistData.Songs = playlistData.Songs.Where(s => allSongs.ContainsKey(s.FullPath)).ToArray();

                MergePlaylist(playlist, playlistData);

                return playlist;
            }).ToArray();

            if (string.IsNullOrWhiteSpace(data.CurrentPlaylistID)) Service.CurrentPlaylist = null;
            else
            {
                Guid currentPlaylistID = Guid.Parse(data.CurrentPlaylistID);
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
            playlist.IsAllShuffle = data.IsAllShuffle;
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
                    Save();
                    await Task.Delay(500);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e);
                }
            }
        }

        private void Save()
        {
            AudioServiceData data = new AudioServiceData(Service);
            StdUtils.XmlSerialize(path, data);
        }

        public void Dispose()
        {
            Unsubscribe();
            disposed = true;

            if (!string.IsNullOrWhiteSpace(path)) Save();
        }
    }
}
