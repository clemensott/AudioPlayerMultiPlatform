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

        private void Init()
        {
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
            TriggerSave();
        }

        private void Service_PlaylistsChanged(object sender, ValueChangedEventArgs<IPlaylistBase[]> e)
        {
            Unsubscribe(e.OldValue);
            Subscribe(e.NewValue);

            TriggerSave();
        }

        private void Load()
        {
            if (!File.Exists(readPath)) return;

            AudioServiceData data = StdUtils.XmlDeserializeFile<AudioServiceData>(readPath);

            Service.Volume = data.Volume;

            MergePlaylist(Service.SourcePlaylist, data.SourcePlaylist, Service.CurrentPlaylist.Songs);
            Service.Playlists = data.Playlists.Select(playlistData =>
            {
                IPlaylistBase newPlaylist = new Playlist(helper);
                MergePlaylist(newPlaylist, playlistData, Service.CurrentPlaylist.Songs);

                return newPlaylist;
            }).Where(p => p.Songs.Length > 0).ToArray();

            Service.CurrentPlaylist = Service.Playlists.ElementAtOrDefault(data.CurrentPlaylistIndex) ?? Service.SourcePlaylist;
        }

        private static void MergePlaylist(IPlaylistBase playlist, PlaylistData data, Song[] allSongs)
        {
            playlist.Songs = MergeSongs(playlist.Songs, data.Songs, allSongs);
            playlist.IsAllShuffle = data.IsAllShuffle;
            playlist.Loop = data.Loop;

            Song currentSong;
            if (playlist.Songs.TryFirst(s => s.FullPath == data.CurrentSongPath, out currentSong))
            {
                playlist.CurrentSong = currentSong;
                playlist.Position = data.Position;
                playlist.Duration = data.Duration;
                playlist.WannaSong = RequestSong.Get(currentSong, data.Position, data.Duration);
            }
        }

        private static Song[] MergeSongs(Song[] currentSongs, string[] oldSongs, Song[] allSongs)
        {
            List<Song> newSongs = new List<Song>();
            List<Song> remainingSongs = currentSongs.ToList();

            foreach (string oldPath in oldSongs)
            {
                Song song;
                if (!allSongs.TryFirst(n => n.FullPath == oldPath, out song)) continue;

                newSongs.Add(song);
                remainingSongs.Remove(song);
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
            StdUtils.XmlSerialize(writePath, data);
        }

        public void Dispose()
        {
            Unsubscribe();
            disposed = true;

            if (!string.IsNullOrWhiteSpace(writePath)) Save();
        }
    }
}
