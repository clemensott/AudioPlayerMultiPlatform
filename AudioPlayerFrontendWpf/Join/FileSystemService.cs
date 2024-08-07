using AudioPlayerBackend.FileSystem;
using System.IO;
using System.Threading.Tasks;

namespace AudioPlayerFrontend.Join
{
    class FileSystemService : IFileSystemService
    {
        public Task<string> CreateFileIfNotExits(string fileName)
        {
            if (!File.Exists(fileName)) File.WriteAllBytes(fileName, new byte[0]);
            return Task.FromResult(Path.GetFullPath(fileName));
        }

        public Task<string> ReadTextFile(string fileName)
        {
            return Task.FromResult(File.ReadAllText(fileName));
        }

        public Task WriteTextFile(string fileName, string text)
        {
            File.WriteAllText(fileName, text);
            return Task.CompletedTask;
        }
    }
}
