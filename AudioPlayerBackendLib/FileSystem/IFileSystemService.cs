using System;
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

        Task ReloadLibrary();

        Task UpdateLibrary();

        Task ReloadSourcePlaylist(Guid id);

        Task UpdateSourcePlaylist(Guid id);
    }
}
