using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AudioPlayerBackend.FileSystem;

namespace AudioPlayerFrontendAvalonia.Join;

public class FileSystemService: IFileSystemService
{
    public Task<string> CreateFileIfNotExits(string fileName)
    {
        if (!File.Exists(fileName)) File.WriteAllBytes(fileName, []);
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

    public Task AppendTextLines(string fileName, IEnumerable<string> lines)
    {
        File.AppendAllLines(fileName, lines);
        return Task.CompletedTask;
    }

    public Task DeleteFile(string path)
    {
        File.Delete(path);
        return Task.CompletedTask;
    }
}