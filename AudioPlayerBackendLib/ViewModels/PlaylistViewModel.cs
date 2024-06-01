using AudioPlayerBackend.Audio;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace AudioPlayerBackend.ViewModels
{
    public class PlaylistViewModel : IPlaylistViewModel
    {
        public Guid Id => throw new NotImplementedException();

        public string Name => throw new NotImplementedException();

        public OrderType Shuffle { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public LoopType Loop { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public double PlaybackRate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public TimeSpan Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public TimeSpan Duration => throw new NotImplementedException();

        public RequestSong RequestSong { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Song CurrentSong => throw new NotImplementedException();

        public IList<Song> Songs => throw new NotImplementedException();

        public event PropertyChangedEventHandler PropertyChanged;

        public Task Dispose()
        {
            throw new NotImplementedException();
        }

        public Task SetPlaylistId(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task Start()
        {
            throw new NotImplementedException();
        }

        public Task Stop()
        {
            throw new NotImplementedException();
        }
    }
}
