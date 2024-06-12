using AudioPlayerBackend.Audio;
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

        OrderType Shuffle { get; set; }

        LoopType Loop { get; set; }

        double PlaybackRate { get; set; }

        TimeSpan Position { get; set; }

        TimeSpan Duration { get; }

        Song? CurrentSong { get; }

        IList<Song> Songs { get; }

        Task SetPlaylistId(Guid? id);

        Task SendRequestSong(RequestSong? requestSong);
    }
}
