using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.FileSystem;
using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.Build;

namespace AudioPlayerFrontend.Join
{
    internal class UpdateLibraryService : BaseUpdateLibraryService<string>
    {
        private static readonly Random ran = new Random();
        private readonly ILibraryRepo libraryRepo;
        private readonly IPlaylistsRepo playlistsRepo;

        public UpdateLibraryService(AudioServicesBuildConfig config, ILibraryRepo libraryRepo, IPlaylistsRepo playlistsRepo)
            : base(config, libraryRepo, playlistsRepo)
        {
            this.libraryRepo = libraryRepo;
            this.playlistsRepo = playlistsRepo;
        }

        protected override async Task CheckFileMediaSourceForPlaylist(ICollection<FileMediaSource> allSources,
            FileMediaSource source, FileMediaSourceRoot root)
        {
            string path = GetFileMediaSourcePath(source, root);
            if (!Directory.Exists(path)) return;

            int rootLength = root.Path.Length;
            await CheckFolders(path);

            async Task CheckFolders(string folderPath)
            {
                foreach (string subFolderPath in Directory.GetDirectories(folderPath))
                {
                    string relativePath = FileMediaSource.NormalizeRelativePath(subFolderPath.Substring(rootLength));
                    if (allSources.Any(s => s.RelativePath == relativePath)) return;

                    await TryCreatePlaylist(root, relativePath);
                    await CheckFolders(subFolderPath);
                }
            }
        }

        protected override Task<IEnumerable<string>> LoadAllFiles(FileMediaSources sources)
        {
            return Task.FromResult(LoadAllFilePaths(sources));
        }

        public static IEnumerable<string> LoadAllFilePaths(FileMediaSources sources)
        {
            try
            {
                return sources.Sources
                    .Select(s => GetFileMediaSourcePath(s, sources.Root))
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .SelectMany(LoadFilePaths).ToArray();
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        }

        private static string GetFileMediaSourcePath(FileMediaSource source, FileMediaSourceRoot root)
        {
            string rootPath = GetPathFromFileMediaSourceRoot(root);
            if (string.IsNullOrWhiteSpace(rootPath)) return null;

            return Path.Combine(rootPath, source.RelativePath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static string GetPathFromFileMediaSourceRoot(FileMediaSourceRoot root)
        {
            switch (root.PathType)
            {
                case FileMediaSourceRootPathType.Path:
                    return root.Path;

                case FileMediaSourceRootPathType.KnownFolder:
                    return GetLocalKnownFolders()
                        .TryFirst(f => f.Value == root.Path, out LocalKnownFolder folder)
                        ? folder.CurrentFullPath : null;

                default:
                    return null;
            }
        }

        private static IEnumerable<string> LoadFilePaths(string path)
        {
            if (File.Exists(path)) yield return path;
            else if (Directory.Exists(path))
            {
                foreach (string file in Directory.GetFiles(path))
                {
                    if (IsNotHidden(file)) yield return file;
                }
            }
        }

        private static bool IsNotHidden(string path)
        {
            FileInfo file = new FileInfo(path);

            return (file.Attributes & FileAttributes.Hidden) == 0;
        }

        protected override Task<Song?> CreateSong(string path)
        {
            return Task.FromResult<Song?>(new Song(path));
        }

        public static IEnumerable<LocalKnownFolder> GetLocalKnownFolders()
        {
            yield return CreateLocalKnownFolder(Environment.SpecialFolder.MyMusic, "My Music");
            yield return CreateLocalKnownFolder(Environment.SpecialFolder.MyVideos, "My Videos");
            yield return CreateLocalKnownFolder(Environment.SpecialFolder.MyPictures, "My Pictures");
            yield return CreateLocalKnownFolder(Environment.SpecialFolder.MyDocuments, "My Documents");
            yield return CreateLocalKnownFolder(Environment.SpecialFolder.DesktopDirectory, "Desktop");
        }

        private static LocalKnownFolder CreateLocalKnownFolder(Environment.SpecialFolder folder, string name)
        {
            string value = folder.ToString();
            string fullPath = Environment.GetFolderPath(folder);

            return new LocalKnownFolder(name, value, fullPath);
        }

        protected override string GetFileFullPath(string file)
        {
            return file;
        }
    }
}
