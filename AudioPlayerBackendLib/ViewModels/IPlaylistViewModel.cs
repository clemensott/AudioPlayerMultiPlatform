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

        string Name { get; }

        PlaylistType Type { get; }

        OrderType Shuffle { get; set; }

        LoopType Loop { get; set; }

        double PlaybackRate { get; set; }

        TimeSpan Position { get; set; }

        TimeSpan Duration { get; }

        Song? CurrentSong { get; }

        RequestSong? RequestedSong { get; set; }

        ICollection<Song> Songs { get; }

        Task SetPlaylistId(Guid? id);

        Task SendRequestSong(RequestSong? requestSong);
    }
}
