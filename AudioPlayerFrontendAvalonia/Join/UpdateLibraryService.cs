using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using AudioPlayerBackend.Build;
using AudioPlayerBackend.FileSystem;
using StdOttStandard.Linq;

namespace AudioPlayerFrontendAvalonia.Join;

internal class UpdateLibraryService : BaseUpdateLibraryService<string>
{
    public UpdateLibraryService(AudioServicesBuildConfig config, ILibraryRepo libraryRepo, IPlaylistsRepo playlistsRepo)
        : base(config, libraryRepo, playlistsRepo)
    {
    }

    protected override async Task CheckRootForNewPlaylists(ICollection<FileMediaSource> allSources,
        FileMediaSourceRoot root, bool withSubFolders)
    {
        string? rootPath = GetPathFromFileMediaSourceRoot(root);
        if (!Directory.Exists(rootPath)) return;

        int rootLength = rootPath.Length;
        await CheckFolders(rootPath);

        async Task CheckFolders(string folderPath)
        {
            string relativePath = FileMediaSource.NormalizeRelativePath(folderPath.Substring(rootLength));
            if (allSources.All(s => s.RelativePath != relativePath))
            {
                await TryCreatePlaylist(root, relativePath);
            }

            if (!withSubFolders) return;
            foreach (string subFolderPath in Directory.GetDirectories(folderPath))
            {
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
                .OfType<string>()
                .SelectMany(LoadFilePaths).ToArray();
        }
        catch
        {
            return [];
        }
    }

    private static string? GetFileMediaSourcePath(FileMediaSource source, FileMediaSourceRoot root)
    {
        string? rootPath = GetPathFromFileMediaSourceRoot(root);
        if (string.IsNullOrWhiteSpace(rootPath)) return null;

        return Path.Combine(rootPath, source.RelativePath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static string? GetPathFromFileMediaSourceRoot(FileMediaSourceRoot root)
    {
        switch (root.PathType)
        {
            case FileMediaSourceRootPathType.Path:
                return root.Path;

            case FileMediaSourceRootPathType.KnownFolder:
                (string root, string releative) split = FileSystemUtils.SplitPath(root.Path);
                return GetLocalKnownFolders()
                    .TryFirst(f => f.Value == split.root, out LocalKnownFolder folder)
                    ? Path.Combine(folder.CurrentFullPath, split.releative)
                    : null;

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