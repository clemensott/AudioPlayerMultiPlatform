using System;

namespace AudioPlayerBackend.Audio
{
    class SourcePlaylist : Playlist, ISourcePlaylist
    {
        private FileMediaSource[] fileMediaSources;

        public event EventHandler<ValueChangedEventArgs<FileMediaSource[]>> FileMediaSourcesChanged;

        public FileMediaSource[] FileMediaSources
        {
            get => fileMediaSources;
            set
            {
                if (value == fileMediaSources) return;

                var args = new ValueChangedEventArgs<FileMediaSource[]>(FileMediaSources, value);
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
