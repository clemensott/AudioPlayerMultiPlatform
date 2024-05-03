using AudioPlayerBackend.Audio;
using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace AudioPlayerFrontend
{
    internal class FileMediaSourcesHelper
    {
        internal struct FileMediaSourceRootPaths
        {
            public string RootPath { get; }

            public IEnumerable<string> Children { get; }

            public FileMediaSourceRootPaths(string rootPath, string child)
            {
                RootPath = rootPath;
                Children = new string[] { child };
            }

            public FileMediaSourceRootPaths(string rootPath, IEnumerable<string> children)
            {
                RootPath = rootPath;
                Children = children;
            }
        }

        private static readonly char[] diretorySeparators = new char[] { '/', '\\' };

        private static string TrimRootPath(string path, string rootPath)
        {
            return path.Length > rootPath.Length
                ? rootPath.Remove(rootPath.Length + 1)
                : string.Empty;
        }

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

        private static IEnumerable<IList<FileMediaSourceRootPaths>> GetPossibleRoots(
            IList<string> paths)
        {
            int depth = 1;
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

        private static (FileMediaSourceRoot root, IEnumerable<FileMediaSource> soruces) CreateFileMediaSources(
            string rootPath, IEnumerable<string> children)
        {
            FileMediaSourceRoot root = new FileMediaSourceRoot()
            {
                Id = Guid.NewGuid(),
                Name = rootPath,
                Type = FileMediaSourceRootType.Path,
                Value = rootPath,
            };

            IEnumerable<FileMediaSource> soruces = children.Select(child => new FileMediaSource()
            {
                RootId = root.Id,
                RelativePath = child,
            });

            return (root, soruces);
        }

        private static (IList<FileMediaSource> newSources, IList<FileMediaSourceRoot> newRoots) GroupFileMediaPaths(IList<string> paths)
        {
            IList<FileMediaSourceRootPaths> lastList = null;
            foreach (IList<FileMediaSourceRootPaths> list in GetPossibleRoots(paths))
            {
                if (lastList != null && lastList.Count < list.Count) break;

                lastList = list;
            }

            if (lastList == null) return (new FileMediaSource[0], new FileMediaSourceRoot[0]);

            var fileMediaPahts = lastList.Select(item => CreateFileMediaSources(item.RootPath, item.Children)).ToArray();
            IList<FileMediaSource> newSources = fileMediaPahts.SelectMany(media => media.soruces).ToArray();
            IList<FileMediaSourceRoot> newRoots = fileMediaPahts.Select(media => media.root).ToArray();

            return (newSources, newRoots);
        }

        public static (IList<FileMediaSource> newSoruces, IList<FileMediaSourceRoot> newRoots) ExtractFileMediaSources(
            IEnumerable<string> paths, IEnumerable<FileMediaSourceRoot> currentRoots)
        {
            FileMediaSourceRoot[] currentPathRoots = currentRoots.Where(r => r.Type == FileMediaSourceRootType.Path).ToArray();
            List<FileMediaSource> newSources = new List<FileMediaSource>();
            List<string> missingRootPaths = new List<string>();
            foreach (string path in paths)
            {
                if (currentPathRoots.TryFirst(r => path.StartsWith(r.Value), out FileMediaSourceRoot matchingRoot))
                {
                    newSources.Add(new FileMediaSource()
                    {
                        RootId = matchingRoot.Id,
                        RelativePath = TrimRootPath(path, matchingRoot.Value),
                    });
                }
                else missingRootPaths.Add(path);
            }

            (IList<FileMediaSource> additionalSources, IList<FileMediaSourceRoot> newRoots) = GroupFileMediaPaths(missingRootPaths);
            newSources.AddRange(additionalSources);

            return (newSources, newRoots);
        }
    }
}
