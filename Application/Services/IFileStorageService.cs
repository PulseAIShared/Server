using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Saves an uploaded file to storage
        /// </summary>
        /// <param name="file">The uploaded file</param>
        /// <param name="folder">The folder to save to</param>
        /// <param name="fileName">The file name to use</param>
        /// <returns>The file path where the file was saved</returns>
        Task<string> SaveFileAsync(IFormFile file, string folder, string fileName);

        /// <summary>
        /// Reads the contents of a file as text
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        /// <returns>The file contents as a string</returns>
        Task<string> ReadFileAsync(string filePath);

        /// <summary>
        /// Reads the contents of a file as bytes
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        /// <returns>The file contents as a byte array</returns>
        Task<byte[]> ReadFileBytesAsync(string filePath);

        /// <summary>
        /// Deletes a file from storage
        /// </summary>
        /// <param name="filePath">The path to the file to delete</param>
        Task DeleteFileAsync(string filePath);

        /// <summary>
        /// Checks if a file exists in storage
        /// </summary>
        /// <param name="filePath">The path to check</param>
        /// <returns>True if the file exists</returns>
        Task<bool> FileExistsAsync(string filePath);

        /// <summary>
        /// Gets the size of a file in bytes
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        /// <returns>The file size in bytes</returns>
        Task<long> GetFileSizeAsync(string filePath);

        /// <summary>
        /// Moves a file from one location to another
        /// </summary>
        /// <param name="sourcePath">The current file path</param>
        /// <param name="destinationPath">The new file path</param>
        Task MoveFileAsync(string sourcePath, string destinationPath);

        /// <summary>
        /// Creates a temporary file path for uploads
        /// </summary>
        /// <param name="folder">The folder for the file</param>
        /// <param name="extension">The file extension</param>
        /// <returns>A unique file path</returns>
        string GenerateFilePath(string folder, string extension);
    }

}
