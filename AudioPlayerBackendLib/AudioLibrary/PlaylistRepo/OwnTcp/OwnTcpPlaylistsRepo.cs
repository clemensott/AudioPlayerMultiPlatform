using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using AudioPlayerBackend.Communication;
using AudioPlayerBackend.Communication.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo.OwnTcp
{
    internal class OwnTcpPlaylistsRepo : IPlaylistsRepo
    {
        private readonly IClientCommunicator clientCommunicator;

        public event EventHandler<PlaylistChangeArgs<string>> OnNameChange;
        public event EventHandler<PlaylistChangeArgs<OrderType>> OnShuffleChange;
        public event EventHandler<PlaylistChangeArgs<LoopType>> OnLoopChange;
        public event EventHandler<PlaylistChangeArgs<double>> OnPlaybackRateChange;
        public event EventHandler<PlaylistChangeArgs<TimeSpan>> OnPositionChange;
        public event EventHandler<PlaylistChangeArgs<TimeSpan>> OnDurationChange;
        public event EventHandler<PlaylistChangeArgs<RequestSong?>> OnRequestSongChange;
        public event EventHandler<PlaylistChangeArgs<Guid?>> OnCurrentSongIdChange;
        public event EventHandler<PlaylistChangeArgs<ICollection<Song>>> OnSongsChange;
        public event EventHandler<InsertPlaylistArgs> OnInsertPlaylist;
        public event EventHandler<RemovePlaylistArgs> OnRemovePlaylist;
        public event EventHandler<PlaylistChangeArgs<FileMediaSources>> OnFileMedisSourcesChange;
        public event EventHandler<PlaylistChangeArgs<DateTime?>> OnFilesLastUpdatedChange;
        public event EventHandler<PlaylistChangeArgs<DateTime?>> OnSongsLastUpdatedChange;

        public OwnTcpPlaylistsRepo(IClientCommunicator clientCommunicator)
        {
            this.clientCommunicator = clientCommunicator;
        }

        public Task Start()
        {
            clientCommunicator.Received += ClientCommunicator_Received;
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            clientCommunicator.Received -= ClientCommunicator_Received;
            return Task.CompletedTask;
        }

        public Task Dispose()
        {
            return Stop();
        }

        private void ClientCommunicator_Received(object sender, ReceivedEventArgs e)
        {
            string[] parts = e.Topic.Split('.');
            if (parts.Length != 2 || parts[0] != nameof(ILibraryRepo)) return;

            ByteQueue payload = e.Payload;
            switch (parts[1])
            {
            }
        }

        public Task<Playlist> GetPlaylist(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task SendInsertPlaylist(Playlist playlist, int? index)
        {
            throw new NotImplementedException();
        }

        public Task SendCurrentSongIdChange(Guid id, Guid currentSongId)
        {
            throw new NotImplementedException();
        }

        public Task SendCurrentSongIdChange(Guid id, Guid? currentSongId)
        {
            throw new NotImplementedException();
        }

        public Task SendDurationChange(Guid id, TimeSpan duration)
        {
            throw new NotImplementedException();
        }

        public Task SendRemovePlaylist(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task SendLoopChange(Guid id, LoopType loop)
        {
            throw new NotImplementedException();
        }

        public Task SendNameChange(Guid id, string name)
        {
            throw new NotImplementedException();
        }

        public Task SendPlaybackRateChange(Guid id, double playbackRate)
        {
            throw new NotImplementedException();
        }

        public Task SendPositionChange(Guid id, TimeSpan position)
        {
            throw new NotImplementedException();
        }

        public Task SendRequestSongChange(Guid id, RequestSong requestSong)
        {
            throw new NotImplementedException();
        }

        public Task SendRequestSongChange(Guid id, RequestSong? requestSong)
        {
            throw new NotImplementedException();
        }

        public Task SendShuffleChange(Guid id, OrderType shuffle)
        {
            throw new NotImplementedException();
        }

        public Task SendSongsChange(Guid id, ICollection<Song> songs)
        {
            throw new NotImplementedException();
        }

        public Task<ICollection<FileMediaSource>> GetFileMediaSourcesOfRoot(Guid rootId)
        {
            throw new NotImplementedException();
        }

        public Task SendFileMedisSourcesChange(Guid id, FileMediaSources fileMediaSources)
        {
            throw new NotImplementedException();
        }

        public Task SendFilesLastUpdatedChange(Guid id, DateTime? filesLastUpdated)
        {
            throw new NotImplementedException();
        }

        public Task SendSongsLastUpdatedChange(Guid id, DateTime? songsLastUpdated)
        {
            throw new NotImplementedException();
        }
    }
}
