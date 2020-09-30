using AudioPlayerBackend.Audio;
using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace AudioPlayerFrontend.Join
{
    class SourcePlaylistHelper : NotifyPropertyChangedHelper, ISourcePlaylistHelper
    {
        private static SourcePlaylistHelper instance;

        public static SourcePlaylistHelper Current
        {
            get
            {
                if (instance == null) instance = new SourcePlaylistHelper();

                return instance;
            }
        }

        public Func<string[], Task<IEnumerable<string>>> FetchFiles => async sources =>
        {
            try
            {
                IEnumerable<string>[] nonHiddenFiles = await Task.WhenAll(sources.ToNotNull().Select(LoadFilePaths));

                return nonHiddenFiles.SelectMany(f => f).ToArray();
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        };

        private async static Task<IEnumerable<string>> LoadFilePaths(string path)
        {
            if (File.Exists(path)) return new string[] { path };
            else if (Directory.Exists(path))
            {
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);
                IReadOnlyList<StorageFile> files = await folder.GetFilesAsync();

                return files.Select(f => f.Path);
            }

            return new string[0];
        }

        public Func<string, Task<Song?>> CreateSong => async f =>
        {
            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(f);
                MusicProperties properties = await file.Properties.GetMusicPropertiesAsync();

                if (properties.Duration == TimeSpan.Zero) return null;
                return new Song(f)
                {
                    Title = string.IsNullOrWhiteSpace(properties.Title) ? file.Name : properties.Title,
                    Artist = string.IsNullOrWhiteSpace(properties.Artist) ? null : properties.Artist,
                };
            }
            catch
            {
                return null;
            }
        };

        private SourcePlaylistHelper()
        {
        }


    }
}
