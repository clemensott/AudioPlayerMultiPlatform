using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using AudioPlayerBackend.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace AudioPlayerFrontend.Join
{
    internal class UpdateLibraryService : BaseUpdateLibraryService<StorageFile>
    {
        private readonly StorageFileEqualityComparer storageFileEqualityComparer;

        public UpdateLibraryService(ILibraryRepo libraryRepo, IPlaylistsRepo playlistsRepo) : base(libraryRepo, playlistsRepo)
        {
            storageFileEqualityComparer = new StorageFileEqualityComparer();
        }

        protected override async Task CheckFileMediaSourceForPlaylist(ICollection<FileMediaSource> allSources,
            FileMediaSource source, FileMediaSourceRoot root)
        {
            IStorageItem rootStorageItem = await GetStorageItemFromFileMediaSourceRoot(root);
            if (!(rootStorageItem is StorageFolder rootFolder)) return;

            await CheckFolders(rootFolder, source.RelativePath);

            async Task CheckFolders(StorageFolder folder, string relativeFolderPath)
            {
                foreach (StorageFolder subFolder in await folder.GetFoldersAsync())
                {
                    string relativeSubFolderPath = FileMediaSource.NormalizeRelativePath(Path.Combine(relativeFolderPath, subFolder.Name));
                    if (allSources.Any(s => s.RelativePath == relativeSubFolderPath)) return;

                    await TryCreatePlaylist(root, relativeSubFolderPath);
                    await CheckFolders(subFolder, relativeSubFolderPath);
                }
            }
        }

        protected override async Task<IEnumerable<StorageFile>> LoadAllFiles(FileMediaSources sources)
        {
            try
            {
                IStorageItem rootStorageItem = await GetStorageItemFromFileMediaSourceRoot(sources.Root);
                if (rootStorageItem is StorageFile file) return new StorageFile[] { file };
                if (rootStorageItem is StorageFolder rootFolder)
                {
                    IEnumerable<StorageFile>[] storageFiles =
                        await Task.WhenAll(sources.Sources.Select(s => LoadStorageFiles(s, rootFolder)));
                    return storageFiles
                        .SelectMany(files => files)
                        .Distinct(storageFileEqualityComparer)
                        .ToArray();
                }

                return Enumerable.Empty<StorageFile>();
            }
            catch
            {
                return Enumerable.Empty<StorageFile>();
            }
        }

        private static async Task<IStorageItem> GetStorageItemFromFileMediaSourceRoot(FileMediaSourceRoot root)
        {
            switch (root.PathType)
            {
                case FileMediaSourceRootPathType.Path:
                    try
                    {
                        return await StorageFolder.GetFolderFromPathAsync(root.Path);
                    }
                    catch
                    {
                        return await StorageFile.GetFileFromPathAsync(root.Path);
                    }

                case FileMediaSourceRootPathType.KnownFolder:
                    KnownFolderId knownFolderId = Enum.Parse<KnownFolderId>(root.Path);
                    return await KnownFolders.GetFolderAsync(knownFolderId);

                default:
                    return null;
            }
        }

        private static async Task<IEnumerable<StorageFile>> LoadStorageFiles(FileMediaSource source, StorageFolder root)
        {
            IStorageItem item = await GetStorageItemFromMediaSourcePath(source, root);

            if (item is StorageFile file) return new StorageFile[] { file };
            else if (item is StorageFolder folder) return await folder.GetFilesAsync();

            return Enumerable.Empty<StorageFile>();
        }

        private static async Task<IStorageItem> GetStorageItemFromMediaSourcePath(FileMediaSource source, StorageFolder root)
        {
            string[] parts = source.RelativePath
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            IStorageItem item = root;
            foreach (string part in parts)
            {
                item = await (item as StorageFolder)?.TryGetItemAsync(part);
            }

            return item;
        }

        protected override async Task<Song?> CreateSong(StorageFile file)
        {
            try
            {
                MusicProperties properties = await file.Properties.GetMusicPropertiesAsync();

                if (properties.Duration == TimeSpan.Zero) return null;
                return new Song(file.Path)
                {
                    Title = string.IsNullOrWhiteSpace(properties.Title) ? file.Name : properties.Title,
                    Artist = string.IsNullOrWhiteSpace(properties.Artist) ? null : properties.Artist,
                };
            }
            catch
            {
                return null;
            }
        }

        protected override string GetFileFullPath(StorageFile file)
        {
            return file.Path;
        }

        class StorageFileEqualityComparer : IEqualityComparer<StorageFile>
        {
            public bool Equals(StorageFile x, StorageFile y)
            {
                return x.Path == y.Path;
            }

            public int GetHashCode(StorageFile obj)
            {
                return obj.Path.GetHashCode();
            }
        }
    }
}
