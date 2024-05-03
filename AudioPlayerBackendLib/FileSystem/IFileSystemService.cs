using AudioPlayerBackend.Audio;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AudioPlayerBackend.FileSystem
{
    public interface IFileSystemService
    {
        /// <summary>
        /// Reads text of file.
        /// </summary>
        /// <param name="fileName">file name or path of file</param>
        /// <returns>read text</returns>
        Task<string> ReadTextFile(string fileName);

        /// <summary>
        /// Writes text to file.
        /// </summary>
        /// <param name="fileName">file name or path of file</param>
        /// <param name="text">text to write</param>
        /// <returns></returns>
        Task WriteTextFile(string fileName, string text);

        Task ReloadSourcePlaylist(ISourcePlaylist playlist, FileMediaSourceRoot[] roots);

        Task UpdateSourcePlaylist(ISourcePlaylist playlist, FileMediaSourceRoot[] roots);

        IEnumerable<LocalKnownFolder> GetLocalKnownFolders();
    }
}
