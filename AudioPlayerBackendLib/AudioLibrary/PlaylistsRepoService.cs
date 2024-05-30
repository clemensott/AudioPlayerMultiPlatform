using AudioPlayerBackend.Audio;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary
{
    class PlaylistsRepoService
    {
        private readonly IPlaylistsRepo repo;
        private readonly IList<Client> clients;

        public PlaylistsRepoService(IPlaylistsRepo repo)
        {
            this.repo = repo;
            clients = new List<Client>();
        }

        public IPlaylistsRepo CreateRepo()
        {
            Client client = new Client(this);
            clients.Add(client);
            return client;
        }

        public bool RemoveRepo(IPlaylistsRepo repo)
        {
            Client client = repo as Client;
            return clients.Remove(client);
        }

        class Client : IPlaylistsRepo
        {
            private readonly PlaylistsRepoService parent;

            public event EventHandler<PlaylistChange<string>> OnNameChange;
            public event EventHandler<PlaylistChange<OrderType>> OnShuffleChange;
            public event EventHandler<PlaylistChange<LoopType>> OnLoopChange;
            public event EventHandler<PlaylistChange<double>> OnPlaybackRateChange;
            public event EventHandler<PlaylistChange<TimeSpan>> OnPositionChange;
            public event EventHandler<PlaylistChange<TimeSpan>> OnDurationChange;
            public event EventHandler<PlaylistChange<RequestSong>> OnRequestSongChange;
            public event EventHandler<PlaylistChange<Guid>> OnCurrentSongIdChange;
            public event EventHandler<PlaylistChange<IList<Song>>> OnSongsChange;

            public Client(PlaylistsRepoService parent)
            {
                this.parent = parent;
            }

            public Task<Playlist> GetPlaylist(Guid id)
            {
                return parent.repo.GetPlaylist(id);
            }

            private void ForEachClientExcept(Action<Client> action)
            {
                foreach (Client client in parent.clients)
                {
                    if (client != this) action(client);
                }
            }

            public Task SendNameChange(Guid id, string name)
            {
                var args = new PlaylistChange<string>(id, name);
                ForEachClientExcept(client => client.OnNameChange?.Invoke(this, args));
                return parent.repo.SendNameChange(id, name);
            }

            public Task SendShuffleChange(Guid id, OrderType shuffle)
            {
                var args = new PlaylistChange<OrderType>(id, shuffle);
                ForEachClientExcept(client => client.OnShuffleChange?.Invoke(this, args));
                return parent.repo.SendShuffleChange(id, shuffle);
            }

            public Task SendLoopChange(Guid id, LoopType loop)
            {
                var args = new PlaylistChange<LoopType>(id, loop);
                ForEachClientExcept(client => client.OnLoopChange?.Invoke(this, args));
                return parent.repo.SendLoopChange(id, loop);
            }

            public Task SendPlaybackRateChange(Guid id, double playbackRate)
            {
                var args = new PlaylistChange<double>(id, playbackRate);
                ForEachClientExcept(client => client.OnPlaybackRateChange?.Invoke(this, args));
                return parent.repo.SendPlaybackRateChange(id, playbackRate);
            }

            public Task SendPositionChange(Guid id, TimeSpan position)
            {
                var args = new PlaylistChange<TimeSpan>(id, position);
                ForEachClientExcept(client => client.OnPositionChange?.Invoke(this, args));
                return parent.repo.SendPositionChange(id, position);
            }

            public Task SendDurationChange(Guid id, TimeSpan duration)
            {
                var args = new PlaylistChange<TimeSpan>(id, duration);
                ForEachClientExcept(client => client.OnDurationChange?.Invoke(this, args));
                return parent.repo.SendDurationChange(id, duration);
            }

            public Task SendRequestSongChange(Guid id, RequestSong requestSong)
            {
                var args = new PlaylistChange<RequestSong>(id, requestSong);
                ForEachClientExcept(client => client.OnRequestSongChange?.Invoke(this, args));
                return parent.repo.SendRequestSongChange(id, requestSong);
            }

            public Task SendCurrentSongIdChange(Guid id, Guid currentSongId)
            {
                var args = new PlaylistChange<Guid>(id, currentSongId);
                ForEachClientExcept(client => client.OnCurrentSongIdChange?.Invoke(this, args));
                return parent.repo.SendCurrentSongIdChange(id, currentSongId);
            }

            public Task SendSongsChange(Guid id, IList<Song> songs)
            {
                var args = new PlaylistChange<IList<Song>>(id, songs);
                ForEachClientExcept(client => client.OnSongsChange?.Invoke(this, args));
                return parent.repo.SendSongsChange(id, songs);
            }
        }
    }
}
