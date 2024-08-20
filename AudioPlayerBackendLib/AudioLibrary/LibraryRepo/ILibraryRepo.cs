﻿using AudioPlayerBackend.Player;
using System;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.LibraryRepo
{
    public interface ILibraryRepo : IAudioService
    {
        Task<Library> GetLibrary();

        Task SendPlayStateChange(PlaybackState playState);
        event EventHandler<AudioLibraryChangeArgs<PlaybackState>> OnPlayStateChange;

        Task SendVolumeChange(double volume);
        event EventHandler<AudioLibraryChangeArgs<double>> OnVolumeChange;

        Task SendCurrentPlaylistIdChange(Guid? currentPlaylistId);
        event EventHandler<AudioLibraryChangeArgs<Guid?>> OnCurrentPlaylistIdChange;

        Task SendFoldersLastUpdatedChange(DateTime? foldersLastUpdated);
        event EventHandler<AudioLibraryChangeArgs<DateTime?>> OnFoldersLastUpdatedChange;
    }
}
