using AudioPlayerBackend.Audio.MediaSource;
using AudioPlayerBackend.Player;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary
{
    class LibraryRepoService
    {
        private readonly ILibraryRepo repo;
        private readonly IList<Client> clients;

        public LibraryRepoService(ILibraryRepo repo)
        {
            this.repo = repo;
            clients = new List<Client>();
        }

        public ILibraryRepo CreateRepo()
        {
            Client client = new Client(this);
            clients.Add(client);
            return client;
        }

        public bool RemoveRepo(ILibraryRepo repo)
        {
            Client client = repo as Client;
            return clients.Remove(client);
        }

        class Client : ILibraryRepo
        {
            private readonly LibraryRepoService parent;

            public event EventHandler<AudioLibraryChange<bool>> OnIsSearchChange;
            public event EventHandler<AudioLibraryChange<bool>> OnIsSearchShuffleChange;
            public event EventHandler<AudioLibraryChange<string>> OnSearchKeyChange;
            public event EventHandler<AudioLibraryChange<PlaybackState>> OnPlayStateChange;
            public event EventHandler<AudioLibraryChange<double>> OnVolumeChange;
            public event EventHandler<AudioLibraryChange<IList<PlaylistInfo>>> OnPlaylistsChange;
            public event EventHandler<AudioLibraryChange<IList<SourcePlaylistInfo>>> OnSourcePlaylistsChange;
            public event EventHandler<AudioLibraryChange<IList<FileMediaSourceRoot>>> OnFileMediaSourceRootsChange;

            public Client(LibraryRepoService parent)
            {
                this.parent = parent;
            }

            public Task SendInitCmd()
            {
                return parent.repo.SendInitCmd();
            }

            private void ForEachClientExcept(Action<Client> action)
            {
                foreach (Client client in parent.clients)
                {
                    if (client != this) action(client);
                }
            }

            public Task SendIsSearchChange(bool isSearch)
            {
                var args = new AudioLibraryChange<bool>(isSearch);
                ForEachClientExcept(client => client.OnIsSearchChange?.Invoke(this, args));
                return parent.repo.SendIsSearchChange(isSearch);
            }

            public Task SendIsSearchShuffleChange(bool isSearchShuffle)
            {
                var args = new AudioLibraryChange<bool>(isSearchShuffle);
                ForEachClientExcept(client => client.OnIsSearchShuffleChange?.Invoke(this, args));
                return parent.repo.SendIsSearchShuffleChange(isSearchShuffle);
            }

            public Task SendSearchKeyChange(string searchKey)
            {
                var args = new AudioLibraryChange<string>(searchKey);
                ForEachClientExcept(client => client.OnSearchKeyChange?.Invoke(this, args));
                return parent.repo.SendSearchKeyChange(searchKey);
            }

            public Task SendPlayStateChange(PlaybackState playState)
            {
                var args = new AudioLibraryChange<PlaybackState>(playState);
                ForEachClientExcept(client => client.OnPlayStateChange?.Invoke(this, args));
                return parent.repo.SendPlayStateChange(playState);
            }

            public Task SendVolumeChange(double volume)
            {
                var args = new AudioLibraryChange<double>(volume);
                ForEachClientExcept(client => client.OnVolumeChange?.Invoke(this, args));
                return parent.repo.SendVolumeChange(volume);
            }

            public Task SendPlaylistsChange(IList<PlaylistInfo> playlists)
            {
                var args = new AudioLibraryChange<IList<PlaylistInfo>>(playlists);
                ForEachClientExcept(client => client.OnPlaylistsChange?.Invoke(this, args));
                return parent.repo.SendPlaylistsChange(playlists);
            }

            public Task SendSourcePlaylistsChange(IList<SourcePlaylistInfo> sourcePlaylists)
            {
                var args = new AudioLibraryChange<IList<SourcePlaylistInfo>>(sourcePlaylists);
                ForEachClientExcept(client => client.OnSourcePlaylistsChange?.Invoke(this, args));
                return parent.repo.SendSourcePlaylistsChange(sourcePlaylists);
            }

            public Task SendFileMediaSourceRootsChange(IList<FileMediaSourceRoot> fileMediaSourceRoots)
            {
                var args = new AudioLibraryChange<IList<FileMediaSourceRoot>>(fileMediaSourceRoots);
                ForEachClientExcept(client => client.OnFileMediaSourceRootsChange?.Invoke(this, args));
                return parent.repo.SendFileMediaSourceRootsChange(fileMediaSourceRoots);
            }
        }
    }
}
