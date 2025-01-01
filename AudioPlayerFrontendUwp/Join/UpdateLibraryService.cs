using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using AudioPlayerBackend.Build;
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

        public UpdateLibraryService(AudioServicesBuildConfig config, ILibraryRepo libraryRepo, IPlaylistsRepo playlistsRepo)
            : base(config, libraryRepo, playlistsRepo)
        {
            storageFileEqualityComparer = new StorageFileEqualityComparer();
        }

        protected override async Task CheckRootForNewPlaylists(ICollection<FileMediaSource> allSources, FileMediaSourceRoot root, bool withSubFolders)
        {
            IStorageItem rootStorageItem = await GetStorageItemFromFileMediaSourceRoot(root);
            if (!(rootStorageItem is StorageFolder rootFolder)) return;

            await CheckFolders(rootFolder, string.Empty);

            async Task CheckFolders(StorageFolder folder, string relativeFolderPath)
            {
                if (!allSources.Any(s => s.RelativePath == relativeFolderPath))
                {
                    await TryCreatePlaylist(root, relativeFolderPath);
                }

                if (!withSubFolders) return;
                foreach (StorageFolder subFolder in await folder.GetFoldersAsync())
                {
                    string relativeSubFolderPath = FileMediaSource
                        .NormalizeRelativePath(Path.Combine(relativeFolderPath, subFolder.Name));
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
                    (string root, string releative) split = FileSystemUtils.SplitPath(root.Path);
                    KnownFolderId knownFolderId = (KnownFolderId)Enum.Parse(typeof(KnownFolderId), split.root, true);

                    StorageFolder knownFolder = GetKnownFolder(knownFolderId);
                    return await GetStorageItemFromMediaSourcePath(knownFolder, split.releative);

                default:
                    return null;
            }
        }

        private static StorageFolder GetKnownFolder(KnownFolderId id)
        {
            switch (id)
            {
                case KnownFolderId.AppCaptures:
                    return KnownFolders.AppCaptures;

                case KnownFolderId.CameraRoll:
                    return KnownFolders.CameraRoll;

                case KnownFolderId.DocumentsLibrary:
                    return KnownFolders.DocumentsLibrary;

                case KnownFolderId.HomeGroup:
                    return KnownFolders.HomeGroup;

                case KnownFolderId.MusicLibrary:
                    return KnownFolders.MusicLibrary;

                case KnownFolderId.Objects3D:
                    return KnownFolders.Objects3D;

                case KnownFolderId.PicturesLibrary:
                    return KnownFolders.PicturesLibrary;

                case KnownFolderId.SavedPictures:
                    return KnownFolders.SavedPictures;

                case KnownFolderId.VideosLibrary:
                    return KnownFolders.VideosLibrary;

                default:
                    return null;
            }
        }

        private static async Task<IEnumerable<StorageFile>> LoadStorageFiles(FileMediaSource source, StorageFolder root)
        {
            IStorageItem item = await GetStorageItemFromMediaSourcePath(root, source.RelativePath);

            if (item is StorageFile file) return new StorageFile[] { file };
            else if (item is StorageFolder folder) return await folder.GetFilesAsync();

            return Enumerable.Empty<StorageFile>();
        }

        private static async Task<IStorageItem> GetStorageItemFromMediaSourcePath(StorageFolder root, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return root;

            string[] parts = relativePath
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
