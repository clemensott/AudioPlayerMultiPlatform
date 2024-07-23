using AudioPlayerBackend.AudioLibrary;
using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using StdOttStandard;
using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace AudioPlayerBackend.ViewModels
{
    public class SongSearchViewModel : ISongSearchViewModel
    {
        private readonly IServicedLibraryRepo libraryRepo;
        private readonly IServicedPlaylistsRepo playlistsRepo;

        public bool IsEnabled => throw new NotImplementedException();

        public bool IsSearching => throw new NotImplementedException();

        public bool IsSearchShuffle
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public string SearchKey
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public IEnumerable<Song> SearchSongs => throw new NotImplementedException();

        public SongSearchViewModel(IServicedLibraryRepo libraryRepo, IServicedPlaylistsRepo playlistsRepo)
        {
            this.libraryRepo = libraryRepo;
            this.playlistsRepo = playlistsRepo;
        }

        private async Task AddSongsToSearchPlaylist(IEnumerable<Song> songs, SearchPlaylistAddType addType, PlaylistInfo searchPlaylist)
        {
            Song[] newSongs;
            Playlist currentPlaylist = await playlistsRepo.GetPlaylist(searchPlaylist.Id);
            switch (addType)
            {
                case SearchPlaylistAddType.FirstInPlaylist:
                    newSongs = songs.Concat(currentPlaylist.Songs).Distinct().ToArray();
                    await playlistsRepo.SendSongsChange(currentPlaylist.Id, newSongs);
                    await playlistsRepo.SendRequestSongChange(currentPlaylist.Id, RequestSong.Start(songs.First()));
                    break;

                case SearchPlaylistAddType.NextInPlaylist:
                    // TODO: implement
                    break;

                case SearchPlaylistAddType.LastInPlaylist:
                    newSongs = currentPlaylist.Songs.Concat(songs).Distinct().ToArray();
                    await playlistsRepo.SendSongsChange(currentPlaylist.Id, newSongs);
                    break;
            }
        }

        private async Task ReplaceSongsInSearchPlaylist(IEnumerable<Song> songs, SearchPlaylistAddType addType,
            Guid? currentPlaylistId, PlaylistInfo searchPlaylist)
        {
            Playlist currentPlaylist = currentPlaylistId.TryHasValue(out Guid id) ? await playlistsRepo.GetPlaylist(id) : null;
            Song? currentSong = currentPlaylist?.GetCurrentSong();
            if (addType == SearchPlaylistAddType.FirstInPlaylist || !currentSong.HasValue)
            {
                await playlistsRepo.SendSongsChange(searchPlaylist.Id, songs.Distinct().ToArray());
                await playlistsRepo.SendRequestSongChange(searchPlaylist.Id, RequestSong.Start(songs.First()));
                await playlistsRepo.SendDurationChange(searchPlaylist.Id, currentPlaylist.Duration);
                await playlistsRepo.SendPositionChange(searchPlaylist.Id, currentPlaylist.Position);

                await libraryRepo.SendCurrentPlaylistIdChange(searchPlaylist.Id);
            }
            else
            {
                Song[] newSongs = songs.Insert(0, currentSong.Value).Distinct().ToArray();
                await playlistsRepo.SendSongsChange(searchPlaylist.Id, newSongs);

                RequestSong requestSong = RequestSong.Get(currentSong.Value, null, currentPlaylist.Duration);
                await playlistsRepo.SendRequestSongChange(searchPlaylist.Id, requestSong);

                await libraryRepo.SendCurrentPlaylistIdChange(searchPlaylist.Id);
            }
        }

        public async Task AddSongsToSearchPlaylist(IEnumerable<Song> songs, SearchPlaylistAddType addType)
        {
            songs = songs as Song[] ?? songs.ToArray();

            if (!songs.Any()) return;

            Library library = await libraryRepo.GetLibrary();
            PlaylistInfo searchPlaylist = library.Playlists.FirstOrDefault(p => p.Type.HasFlag(PlaylistType.Search));

            if (searchPlaylist == null)
            {
                IPlaylist playlist = AudioPlayerServiceProvider.Current.GetAudioCreateService().CreatePlaylist(Guid.NewGuid());
                playlist.Name = "Custom";
                playlist.Loop = LoopType.Next;
                playlist.Shuffle = OrderType.Custom;

                if (prepend || !currentSong.HasValue)
                {
                    playlist.Songs = songs.ToArray();
                    playlist.WannaSong = RequestSong.Start(songs.First());
                    playlist.Duration = currentPlaylist.Duration;
                    playlist.Position = currentPlaylist.Position;

                    service.Playlists.Add(playlist);
                    service.CurrentPlaylist = playlist;
                }
                else
                {
                    playlist.Songs = songs.Insert(0, currentSong.Value).ToArray();
                    playlist.WannaSong = RequestSong.Get(currentSong.Value, null, currentPlaylist.Duration);

                    service.Playlists.Add(playlist);
                    service.CurrentPlaylist = playlist;

                    currentPlaylist.CurrentSong = currentPlaylist.Songs.Cast<Song?>()
                        .NextOrDefault(currentSong).next;
                    currentPlaylist.Position = TimeSpan.Zero;
                    currentPlaylist.WannaSong = RequestSong.Start(currentPlaylist.CurrentSong);
                }
            }
            else if (searchPlaylist.Id == library.CurrentPlaylistId)
            {
                await AddSongsToSearchPlaylist(songs, addType, searchPlaylist);
            }
            else
            {
                await ReplaceSongsInSearchPlaylist(songs, addType, library.CurrentPlaylistId, searchPlaylist);
            }
        }

        public void Disable()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Enable()
        {
            throw new NotImplementedException();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
