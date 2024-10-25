using AudioPlayerBackend.FileSystem;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace AudioPlayerFrontend.Join
{
    class FileSystemService : IFileSystemService
    {
        public FileSystemService()
        {
        }

        public async Task<string> CreateFileIfNotExits(string fileName)
        {
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
            return file.Path;
        }

        public async Task<string> ReadTextFile(string fileName)
        {
            StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);
            return await FileIO.ReadTextAsync(file);
        }

        public async Task WriteTextFile(string fileName, string text)
        {
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
            await FileIO.WriteTextAsync(file, text);
        }
    }
}
