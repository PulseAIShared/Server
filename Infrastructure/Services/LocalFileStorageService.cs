using Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class LocalFileStorageService(
      IConfiguration configuration,
      ILogger<LocalFileStorageService> logger) : IFileStorageService
    {
        private readonly string _basePath = configuration["FileStorage:BasePath"] ?? "uploads";
        private readonly string _tempPath = configuration["FileStorage:TempPath"] ?? "temp";

        public async Task<string> SaveFileAsync(IFormFile file, string folder, string fileName)
        {
            try
            {
                var folderPath = Path.Combine(_basePath, folder);
                Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                logger.LogInformation("File saved to {FilePath}", filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to save file {FileName} to folder {Folder}", fileName, folder);
                throw;
            }
        }

        public async Task<string> ReadFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                var content = await File.ReadAllTextAsync(filePath);
                logger.LogDebug("Read file {FilePath} ({Length} characters)", filePath, content.Length);
                return content;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to read file {FilePath}", filePath);
                throw;
            }
        }

        public async Task<byte[]> ReadFileBytesAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                var bytes = await File.ReadAllBytesAsync(filePath);
                logger.LogDebug("Read file {FilePath} ({Length} bytes)", filePath, bytes.Length);
                return bytes;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to read file bytes {FilePath}", filePath);
                throw;
            }
        }

        public async Task DeleteFileAsync(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                    logger.LogInformation("Deleted file {FilePath}", filePath);
                }
                else
                {
                    logger.LogWarning("Attempted to delete non-existent file {FilePath}", filePath);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete file {FilePath}", filePath);
                throw;
            }
        }

        public async Task<bool> FileExistsAsync(string filePath)
        {
            try
            {
                return await Task.FromResult(File.Exists(filePath));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to check if file exists {FilePath}", filePath);
                return false;
            }
        }

        public async Task<long> GetFileSizeAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                var fileInfo = new FileInfo(filePath);
                return await Task.FromResult(fileInfo.Length);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get file size {FilePath}", filePath);
                throw;
            }
        }

        public async Task MoveFileAsync(string sourcePath, string destinationPath)
        {
            try
            {
                if (!File.Exists(sourcePath))
                {
                    throw new FileNotFoundException($"Source file not found: {sourcePath}");
                }

                // Ensure destination directory exists
                var destDirectory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destDirectory))
                {
                    Directory.CreateDirectory(destDirectory);
                }

                await Task.Run(() => File.Move(sourcePath, destinationPath));
                logger.LogInformation("Moved file from {SourcePath} to {DestinationPath}", sourcePath, destinationPath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to move file from {SourcePath} to {DestinationPath}", sourcePath, destinationPath);
                throw;
            }
        }

        public string GenerateFilePath(string folder, string extension)
        {
            var fileName = $"{Guid.NewGuid()}{extension}";
            var folderPath = Path.Combine(_basePath, folder);
            return Path.Combine(folderPath, fileName);
        }
    }
}
