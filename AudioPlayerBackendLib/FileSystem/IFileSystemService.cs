using System.Threading.Tasks;

namespace AudioPlayerBackend.FileSystem
{
    public interface IFileSystemService
    {
        /// <summary>
        /// Creates an empty file.
        /// </summary>
        /// <param name="fileName">File name or path of file</param>
        /// <returns>Absolut file path of File</returns>
        Task<string> CreateFileIfNotExits(string fileName);

        /// <summary>
        /// Reads text of file.
        /// </summary>
        /// <param name="fileName">File name or path of file</param>
        /// <returns>Read text</returns>
        Task<string> ReadTextFile(string fileName);

        /// <summary>
        /// Writes text to file.
        /// </summary>
        /// <param name="fileName">File name or path of file</param>
        /// <param name="text">Text to write</param>
        /// <returns></returns>
        Task WriteTextFile(string fileName, string text);
    }
}
