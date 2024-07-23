using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AudioPlayerFrontend
{
    internal class FileMediaSourcesHelper
    {
        internal class FileMediaSourceRootPaths
        {
            public string RootPath { get; }

            public IEnumerable<string> Children { get; }

            public FileMediaSourceRootPaths(string rootPath, IEnumerable<string> children)
            {
                RootPath = rootPath;
                Children = children;
            }
        }

        private static readonly char[] diretorySeparators = new char[] { '/', '\\' };

        private static (string root, string releative) SplitPath(string path, int depth)
        {
            int index = 0;
            while (index < path.Length && depth > 0)
            {
                if (diretorySeparators.Contains(path[index])) depth--;
                index++;
            }

            if (index + 1 >= path.Length) return (path, string.Empty);

            string root = path.Remove(index - 1);
            string relative = path.Substring(index);

            return (root, relative);
        }

        private static IEnumerable<IList<FileMediaSourceRootPaths>> GetPossibleRoots(ICollection<string> paths)
        {
            int depth = 0;
            while (true)
            {
                var groups = paths.Select(path => SplitPath(path, depth))
                    .GroupBy(t => t.root, (key, g) => new FileMediaSourceRootPaths(key, g.Select(t => t.releative)))
                    .ToArray();

                yield return groups;

                if (groups.All(r => r.Children.All(rel => rel.Length == 0))) yield break;

                depth++;
            }
        }

        private static FileMediaSources CreateFileMediaSources(string rootPath, IEnumerable<string> children)
        {
            ICollection<FileMediaSource> sources = children.Select(child => new FileMediaSource(child)).ToArray();
            FileMediaSourceRoot root = new FileMediaSourceRoot(Guid.NewGuid(), FileMediaSourceRootUpdateType.Files,
                rootPath, FileMediaSourceRootType.Path, rootPath);
            return new FileMediaSources(root, sources);
        }

        public static FileMediaSources ExtractFileMediaSources(ICollection<string> paths)
        {
            if (paths == null) throw new ArgumentNullException(nameof(paths));
            if (paths.Count == 0) throw new ArgumentException($"{nameof(paths)} has to have at leas one value");

            FileMediaSourceRootPaths lastRoot = null;
            foreach (IList<FileMediaSourceRootPaths> list in GetPossibleRoots(paths))
            {
                if (lastRoot != null && list.Count > 1) break;

                lastRoot = list.First();
            }

            if (lastRoot == null) throw new Exception("Could not extract root from paths");

            return CreateFileMediaSources(lastRoot.RootPath, lastRoot.Children);
        }
    }
}
