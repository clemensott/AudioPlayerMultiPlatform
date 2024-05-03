using AudioPlayerBackend.Audio;
using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        private static IEnumerable<IList<FileMediaSourceRootPaths>> GetPossibleRoots(
            IList<string> paths)
        {
            int depth = 1;
            while (true)
            {
                IGrouping<string, FileMediaSourceRootPaths>[] groups = paths.Select(path =>
                {
                    string[] pathParts = path.Split(diretorySeparators, depth + 1);
                    string rootPath = Path.Combine(pathParts.Take(depth).ToArray());

                    return new FileMediaSourceRootPaths(rootPath, pathParts.Skip(depth).FirstOrDefault());
                }).GroupBy(t => t.RootPath).ToArray();

                yield return groups
                    .Select(g => new FileMediaSourceRootPaths(g.Key, g.SelectMany(t => t.Children)))
                    .ToArray();

                if (groups.Length == paths.Count) yield break;

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
                if (lastList != null && lastList.Count < list.Count)
                {
                    var fileMediaPahts = list.Select(item => CreateFileMediaSources(item.RootPath, item.Children)).ToArray();
                    IList<FileMediaSource> newSources = fileMediaPahts.SelectMany(media => media.soruces).ToArray();
                    IList<FileMediaSourceRoot> newRoots = fileMediaPahts.Select(media => media.root).ToArray();

                    return (newSources, newRoots);
                }

                lastList = list;
            }

            return (new FileMediaSource[0], new FileMediaSourceRoot[0]);
        }

        public static (IList<FileMediaSource> newSoruces, IList<FileMediaSourceRoot> newRoots) ExtractFileMediaSources(
            IEnumerable<string> paths, FileMediaSourceRoot[] currentRoots)
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
