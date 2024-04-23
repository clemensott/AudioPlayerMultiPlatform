using System;

namespace AudioPlayerBackend.Audio
{
    class SourcePlaylist : Playlist, ISourcePlaylist
    {
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
