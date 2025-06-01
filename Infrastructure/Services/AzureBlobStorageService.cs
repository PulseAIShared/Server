using Application.Services;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Infrastructure.Services
{
    public class AzureBlobStorageService(
        IConfiguration configuration,
        ILogger<AzureBlobStorageService> logger) : IFileStorageService
    {
        private readonly string _connectionString = configuration.GetConnectionString("AzureBlobStorage")
            ?? throw new InvalidOperationException("Azure Blob Storage connection string not found");
        private readonly string _containerName = configuration["FileStorage:ContainerName"] ?? "uploads";

        private async Task<BlobContainerClient> GetContainerClientAsync()
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
            return containerClient;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folder, string fileName)
        {
            try
            {
                var containerClient = await GetContainerClientAsync();
                var blobPath = $"{folder}/{fileName}";
                var blobClient = containerClient.GetBlobClient(blobPath);

                var blobHttpHeaders = new BlobHttpHeaders
                {
                    ContentType = file.ContentType
                };

                using var stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, new BlobUploadOptions
                {
                    HttpHeaders = blobHttpHeaders
                });

                logger.LogInformation("File uploaded to Azure Blob Storage: {BlobPath}", blobPath);
                return blobPath;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to upload file {FileName} to Azure Blob Storage", fileName);
                throw;
            }
        }

        public async Task<string> ReadFileAsync(string filePath)
        {
            try
            {
                var containerClient = await GetContainerClientAsync();
                var blobClient = containerClient.GetBlobClient(filePath);

                if (!await blobClient.ExistsAsync())
                {
                    throw new FileNotFoundException($"Blob not found: {filePath}");
                }

                var response = await blobClient.DownloadContentAsync();
                var content = response.Value.Content.ToString();

                logger.LogDebug("Downloaded blob {FilePath} ({Length} characters)", filePath, content.Length);
                return content;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to download blob {FilePath}", filePath);
                throw;
            }
        }

        public async Task<byte[]> ReadFileBytesAsync(string filePath)
        {
            try
            {
                var containerClient = await GetContainerClientAsync();
                var blobClient = containerClient.GetBlobClient(filePath);

                if (!await blobClient.ExistsAsync())
                {
                    throw new FileNotFoundException($"Blob not found: {filePath}");
                }

                var response = await blobClient.DownloadContentAsync();
                var bytes = response.Value.Content.ToArray();

                logger.LogDebug("Downloaded blob {FilePath} ({Length} bytes)", filePath, bytes.Length);
                return bytes;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to download blob bytes {FilePath}", filePath);
                throw;
            }
        }

        public async Task DeleteFileAsync(string filePath)
        {
            try
            {
                var containerClient = await GetContainerClientAsync();
                var blobClient = containerClient.GetBlobClient(filePath);

                await blobClient.DeleteIfExistsAsync();
                logger.LogInformation("Deleted blob {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete blob {FilePath}", filePath);
                throw;
            }
        }

        public async Task<bool> FileExistsAsync(string filePath)
        {
            try
            {
                var containerClient = await GetContainerClientAsync();
                var blobClient = containerClient.GetBlobClient(filePath);
                var response = await blobClient.ExistsAsync();
                return response.Value;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to check if blob exists {FilePath}", filePath);
                return false;
            }
        }

        public async Task<long> GetFileSizeAsync(string filePath)
        {
            try
            {
                var containerClient = await GetContainerClientAsync();
                var blobClient = containerClient.GetBlobClient(filePath);

                if (!await blobClient.ExistsAsync())
                {
                    throw new FileNotFoundException($"Blob not found: {filePath}");
                }

                var properties = await blobClient.GetPropertiesAsync();
                return properties.Value.ContentLength;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get blob size {FilePath}", filePath);
                throw;
            }
        }

        public async Task MoveFileAsync(string sourcePath, string destinationPath)
        {
            try
            {
                var containerClient = await GetContainerClientAsync();
                var sourceBlobClient = containerClient.GetBlobClient(sourcePath);
                var destBlobClient = containerClient.GetBlobClient(destinationPath);

                if (!await sourceBlobClient.ExistsAsync())
                {
                    throw new FileNotFoundException($"Source blob not found: {sourcePath}");
                }

                // For modern Azure SDK, we can use direct copy operation
                // Copy the blob content directly
                var copyOperation = await destBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);

                // Wait for copy to complete with timeout
                var timeout = TimeSpan.FromMinutes(5);
                var cancellationToken = new CancellationTokenSource(timeout).Token;

                await copyOperation.WaitForCompletionAsync(cancellationToken);

                if (copyOperation.HasCompleted && !copyOperation.HasValue)
                {
                    throw new InvalidOperationException($"Copy operation failed for blob {sourcePath}");
                }

                // Delete source blob after successful copy
                await sourceBlobClient.DeleteAsync();
                logger.LogInformation("Moved blob from {SourcePath} to {DestinationPath}", sourcePath, destinationPath);
            }
            catch (OperationCanceledException)
            {
                logger.LogError("Timeout occurred while moving blob from {SourcePath} to {DestinationPath}", sourcePath, destinationPath);
                throw new TimeoutException($"Copy operation timed out for blob {sourcePath}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to move blob from {SourcePath} to {DestinationPath}", sourcePath, destinationPath);
                throw;
            }
        }
        public string GenerateFilePath(string folder, string extension)
        {
            var fileName = $"{Guid.NewGuid()}{extension}";
            return $"{folder}/{fileName}";
        }
    }
}
