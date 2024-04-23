using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Audio
{
    class SourcePlaylist : Playlist, ISourcePlaylist
    {
        private static readonly Random ran = new Random();

        private readonly ISourcePlaylistHelper helper;
        private string[] fileMediaSources;

        public event EventHandler<ValueChangedEventArgs<string[]>> FileMediaSourcesChanged;

        public string[] FileMediaSources
        {
            get => fileMediaSources;
            set
            {
                if (value == fileMediaSources) return;

                var args = new ValueChangedEventArgs<string[]>(FileMediaSources, value);
                fileMediaSources = value;
                FileMediaSourcesChanged?.Invoke(this, args);

                OnPropertyChanged(nameof(FileMediaSources));
            }
        }

        public SourcePlaylist(Guid id) : base(id)
        {
        }
    }
}
