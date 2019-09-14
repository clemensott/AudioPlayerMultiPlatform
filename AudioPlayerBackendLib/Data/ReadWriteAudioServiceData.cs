using System;
using System.Collections.Generic;
using System.Linq;
using AudioPlayerBackend.Audio;
using StdOttStandard;

namespace AudioPlayerBackend.Data
{
    public class ReadWriteAudioServiceData : IDisposable
    {
        private static readonly Random rnd = new Random();

        private readonly INotifyPropertyChangedHelper helper;
        private readonly string readPath, writePath;

        public IAudioServiceBase Service { get; }

        public ReadWriteAudioServiceData(string readPath, string writePath,
            IAudioServiceBase service, INotifyPropertyChangedHelper helper = null)
        {
            this.readPath = readPath;
            this.writePath = writePath;
            Service = service;
            this.helper = helper;

            if (!string.IsNullOrWhiteSpace(readPath)) Load();
            if (!string.IsNullOrWhiteSpace(writePath)) Subscribe(Service);
        }

        private void Subscribe(IAudioServiceBase service)
        {
            if (service == null) return;

            service.CurrentPlaylistChanged += Save;
            service.PlaylistsChanged += Service_PlaylistsChanged;
            service.VolumeChanged += Save;

            Subscribe(service.CurrentPlaylist);
            Subscribe(service.Playlists);
        }

        private void Unsubscribe(IAudioServiceBase service)
        {
            if (service == null) return;

            service.CurrentPlaylistChanged -= Save;
            service.PlaylistsChanged -= Service_PlaylistsChanged;
            service.VolumeChanged -= Save;

            Unsubscribe(service.CurrentPlaylist);
            Unsubscribe(service.Playlists);
        }

        private void Subscribe(IPlaylistBase playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged += Save;
            playlist.IsAllShuffleChanged += Save;
            playlist.LoopChanged += Save;
            playlist.PositionChanged += Save;
            playlist.DurationChanged += Save;
            playlist.SongsChanged += Save;
        }

        private void Unsubscribe(IPlaylistBase playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged -= Save;
            playlist.IsAllShuffleChanged -= Save;
            playlist.LoopChanged -= Save;
            playlist.PositionChanged -= Save;
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
            AudioServiceData data = Utils.XmlDeserializeFile<AudioServiceData>(readPath);

            Service.Volume = data.Volume;

            MergePlaylist(Service.SourcePlaylist, data.SourcePlaylist);
            Service.Playlists = data.Playlists.Select(p => MergePlaylist(new Playlist(helper), p)).ToArray();

            Service.CurrentPlaylist = Service.Playlists.ElementAtOrDefault(data.CurrentPlaylistIndex) ?? Service.SourcePlaylist;
        }

        private IPlaylistBase MergePlaylist(IPlaylistBase playlist, PlaylistData data)
        {
            System.Diagnostics.Debug.WriteLine("Merge: {0}; {1}", playlist.CurrentSong, playlist.Songs.Length);

            playlist.Songs = MergeSongs(playlist.Songs, data.Songs);

            Song currentSong;
            if (playlist.Songs.TryFirst(s => s.FullPath == data.CurrentSongPath, out currentSong))
            {
                System.Diagnostics.Debug.WriteLine("LoadCurrentSong: {0}\r\n{1}", currentSong, Service.CurrentPlaylist.Duration);
                playlist.CurrentSong = currentSong;
            }

            playlist.IsAllShuffle = data.IsAllShuffle;
            playlist.Loop = data.Loop;
            playlist.Duration = data.Duration;
            playlist.Position = data.Position;

            System.Diagnostics.Debug.WriteLine("Merged: {0}; {1}; {2}", playlist.CurrentSong, currentSong, playlist.Songs.Length);

            return playlist;
        }

        private static Song[] MergeSongs(Song[] currentSongs, Song[] oldSongs)
        {
            List<Song> newSongs = oldSongs.Where(o => currentSongs.Any(c => c.FullPath == o.FullPath)).ToList();

            foreach (Song newSong in currentSongs.Where(c => oldSongs.All(o => o.FullPath != c.FullPath)))
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
            System.Diagnostics.Debug.WriteLine("Save: " + DateTime.Now.GetDateTimeFormats()[44]);
            AudioServiceData data = new AudioServiceData(Service);
            Utils.XmlSerialize(writePath, data);
        }

        public void Dispose()
        {
            Save();
            Unsubscribe(Service);
        }
    }
}
