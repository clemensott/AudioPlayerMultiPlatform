using AudioPlayerBackend.Player;
using System;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.LibraryRepo
{
    public interface ILibraryRepo : IAudioService
    {
        Task<Library> GetLibrary();

        Task SetPlayState(PlaybackState playState);
        event EventHandler<AudioLibraryChangeArgs<PlaybackState>> PlayStateChanged;

        Task SetVolume(double volume);
        event EventHandler<AudioLibraryChangeArgs<double>> VolumeChanged;

        Task SetCurrentPlaylistId(Guid? currentPlaylistId);
        event EventHandler<AudioLibraryChangeArgs<Guid?>> CurrentPlaylistIdChanged;

        Task SetFoldersLastUpdated(DateTime? foldersLastUpdated);
        event EventHandler<AudioLibraryChangeArgs<DateTime?>> FoldersLastUpdatedChanged;
    }
}
