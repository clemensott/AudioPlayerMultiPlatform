using AudioPlayerBackend.AudioLibrary;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace AudioPlayerBackend.ViewModels
{
    public interface IPlaylistViewModel : IAudioService, INotifyPropertyChanged
    {
        Guid? Id { get; }
        
        bool IsLoaded { get; }

        string Name { get; }

        PlaylistType Type { get; }

        OrderType Shuffle { get; set; }

        LoopType Loop { get; set; }

        double PlaybackRate { get; set; }

        Song? CurrentSong { get; }

        SongRequest? CurrentSongRequest { get; set; }
        
        TimeSpan? Position { get; }
        
        TimeSpan? Duration { get; }

        ICollection<Song> Songs { get; }

        int GetIndexOfSong(Song song);

        Task SetPlaylistId(Guid? id);

        Task SetCurrentSongRequest(SongRequest? requestSong);

        Task RemoveSong(Guid songId);

        Task ClearSongs();
    }
}
