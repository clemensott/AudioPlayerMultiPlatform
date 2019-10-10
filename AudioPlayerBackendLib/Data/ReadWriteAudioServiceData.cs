using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AudioPlayerBackend.Audio;
using StdOttStandard;

namespace AudioPlayerBackend.Data
{
    public class ReadWriteAudioServiceData : IDisposable
    {
        private static readonly Random rnd = new Random();

        private readonly INotifyPropertyChangedHelper helper;
        private readonly string readPath, writePath;
        private SemaphoreSlim saveSem;
        private bool disposed;

        public IAudioServiceBase Service { get; }

        public ReadWriteAudioServiceData(string readPath, string writePath,
            IAudioServiceBase service, INotifyPropertyChangedHelper helper = null)
        {
            this.readPath = readPath;
            this.writePath = writePath;
            Service = service;
            this.helper = helper;

            Init();
        }

        private async void Init()
        {
            await Task.Delay(1000);

            if (!string.IsNullOrWhiteSpace(readPath)) Load();
            if (!string.IsNullOrWhiteSpace(writePath))
            {
                saveSem = new SemaphoreSlim(0);
                Task.Run(SaveHandler);
                Subscribe();
            }
        }

        private void Subscribe()
        {
            if (Service == null) return;

            Service.CurrentPlaylistChanged += Save;
            Service.PlaylistsChanged += Service_PlaylistsChanged;
            Service.VolumeChanged += Save;

            Subscribe(Service.CurrentPlaylist);
            Subscribe(Service.Playlists);
        }

        private void Unsubscribe()
        {
            if (Service == null) return;

            Service.CurrentPlaylistChanged -= Save;
            Service.PlaylistsChanged -= Service_PlaylistsChanged;
            Service.VolumeChanged -= Save;

            Unsubscribe(Service.CurrentPlaylist);
            Unsubscribe(Service.Playlists);
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

        private void Subscribe(IPlaylistBase[] playlists)
        {
            foreach (IPlaylistBase playlist in playlists.ToNotNull())
            {
                Subscribe(playlist);
            }
        }

        private void Unsubscribe(IPlaylistBase[] playlists)
        {
            foreach (IPlaylistBase playlist in playlists.ToNotNull())
            {
                Unsubscribe(playlist);
            }
        }

        private void Save(object sender, EventArgs args)
        {
            Save();
        }

        private void Service_PlaylistsChanged(object sender, ValueChangedEventArgs<IPlaylistBase[]> e)
        {
            Unsubscribe(e.OldValue);
            Subscribe(e.NewValue);

            Save();
        }

        private void Load()
        {
            if (!File.Exists(readPath)) return;

            AudioServiceData data = Utils.XmlDeserializeFile<AudioServiceData>(readPath);

            Service.Volume = data.Volume;

            MergePlaylist(Service.SourcePlaylist, data.SourcePlaylist);
            Service.Playlists = data.Playlists.Select(p => MergePlaylist(new Playlist(helper), p)).ToArray();

            Service.CurrentPlaylist = Service.Playlists.ElementAtOrDefault(data.CurrentPlaylistIndex) ?? Service.SourcePlaylist;
        }

        private static IPlaylistBase MergePlaylist(IPlaylistBase playlist, PlaylistData data)
        {
            playlist.Songs = MergeSongs(playlist.Songs, data.Songs);

            Song currentSong;
            if (playlist.Songs.TryFirst(s => s.FullPath == data.CurrentSongPath, out currentSong))
            {
                playlist.CurrentSong = currentSong;
            }

            playlist.IsAllShuffle = data.IsAllShuffle;
            playlist.Loop = data.Loop;
            playlist.Duration = data.Duration;
            playlist.Position = data.Position;

            return playlist;
        }

        private static Song[] MergeSongs(Song[] currentSongs, string[] oldSongs)
        {
            List<Song> newSongs = new List<Song>();
            List<Song> remainingSongs = currentSongs.ToList();

            foreach (string oldPath in oldSongs)
            {
                int index = remainingSongs.FindIndex(n => n.FullPath == oldPath);

                if (index == -1) continue;

                newSongs.Add(remainingSongs[index]);
                remainingSongs.RemoveAt(index);
            }

            foreach (Song newSong in remainingSongs)
            {
                newSongs.Insert(rnd.Next(newSongs.Count), newSong);
            }

            for (int i = 0; i < newSongs.Count; i++)
            {
                Song song = newSongs[i];
                song.Index = i + 1;
                newSongs[i] = song;
            }

            return newSongs.ToArray();
        }

        private void Save()
        {
            saveSem.Release();
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

                    AudioServiceData data = new AudioServiceData(Service);
                    Utils.XmlSerialize(writePath, data);

                    await Task.Delay(500);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e);
                }
            }
        }

        public void Dispose()
        {
            Unsubscribe();
            Save();
            disposed = true;
        }
    }
}
