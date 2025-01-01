using System.IO;
using System.Linq;

namespace AudioPlayerBackend.FileSystem
{
    public static class FileSystemUtils
    {
        private static readonly char[] diretorySeparators = new char[] {
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar,
        };

        public static (string root, string releative) SplitPath(string path, int depth = 1)
        {
            int index = 0;
            while (index < path.Length && depth > 0)
            {
                if (diretorySeparators.Contains(path[index])) depth--;
                index++;
            }

            if (index + 1 >= path.Length) return (path, string.Empty);

            string root = index - 1 >= 0 ? path.Remove(index - 1) : string.Empty;
            string relative = path.Substring(index);

            return (root, relative);
        }
    }
}
