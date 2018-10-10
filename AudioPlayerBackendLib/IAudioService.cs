using System;
using System.ServiceModel;
using System.Windows.Controls;

namespace AudioPlayerBackendLib
{
    [ServiceContract]
    public interface IAudioService
    {
        [OperationContract]
        string GetDebugInfo();

        MediaElement GetMediaElement();

        [OperationContract]
        States GetStates();

        [OperationContract]
        TimeSpan GetPositon();

        [OperationContract]
        void SetPosition(TimeSpan position);

        [OperationContract]
        TimeSpan GetDuration();

        [OperationContract]
        PlayState GetPlayState();

        [OperationContract]
        void SetPlayState(PlayState state);

        [OperationContract]
        void Play();

        [OperationContract]
        void Pause();

        [OperationContract]
        void Stop();

        [OperationContract]
        bool GetIsAllShuffle();

        [OperationContract]
        void SetIsAllShuffle(bool isAllShuffle);

        [OperationContract]
        bool GetIsSearchShuffle();

        [OperationContract]
        void SetIsSearchShuffle(bool isSearchShuffle);

        [OperationContract]
        bool GetIsOnlySearch();

        [OperationContract]
        void SetIsOnlySearch(bool isOnlySearch);

        [OperationContract]
        string GetSearchKey();

        [OperationContract]
        void SetSearchKey(string searchKey);

        [OperationContract]
        string[] GetMediaSources();

        [OperationContract]
        void SetMediaSources(string[] sources);

        [OperationContract]
        Song GetCurrentSong();

        [OperationContract]
        void SetCurrentSong(Song song);

        [OperationContract]
        Song[] GetAllSongs();

        [OperationContract]
        Song[] GetSearchSongs();

        [OperationContract]
        void Next();

        [OperationContract]
        void Previous();

        [OperationContract]
        void Refresh();

        [OperationContract]
        void CloseInstance();
    }
}
