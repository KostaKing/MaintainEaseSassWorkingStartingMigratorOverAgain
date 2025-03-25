using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace MaintainEase.Infrastructure.Files
{
    /// <summary>
    /// Implementation of file storage service using local file system
    /// </summary>
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly string _basePath;
        private readonly string _baseUrl;

        public LocalFileStorageService(IConfiguration configuration)
        {
            _basePath = configuration["FileStorage:LocalBasePath"];
            _baseUrl = configuration["FileStorage:BaseUrl"];
            
            // Ensure base directory exists
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder = null)
        {
            // Create file ID and sanitize file name
            var fileId = Guid.NewGuid().ToString();
            var sanitizedFileName = SanitizeFileName(fileName);
            
            // Create folder if specified
            var targetFolder = _basePath;
            if (!string.IsNullOrEmpty(folder))
            {
                targetFolder = Path.Combine(_basePath, folder);
                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }
            }
            
            // Write file with a unique name (ID + original name)
            var filePath = Path.Combine(targetFolder, $"{fileId}_{sanitizedFileName}");
            using (var fileStream2 = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fileStream2);
            }
            
            // Create metadata file
            var metadata = new FileMetadata
            {
                Id = fileId,
                FileName = sanitizedFileName,
                ContentType = contentType,
                Size = fileStream.Length,
                UploadDate = DateTimeOffset.UtcNow,
                Folder = folder,
                Url = GetFileUrl(fileId)
            };
            
            // Save metadata
            await SaveMetadataAsync(metadata);
            
            return fileId;
        }

        public async Task<byte[]> DownloadFileAsync(string fileId)
        {
            var metadata = await GetFileMetadataAsync(fileId);
            if (metadata == null)
            {
                throw new FileNotFoundException($"File with ID {fileId} not found");
            }
            
            var filePath = GetFilePath(fileId, metadata.FileName, metadata.Folder);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File {filePath} not found");
            }
            
            return await File.ReadAllBytesAsync(filePath);
        }

        public async Task DeleteFileAsync(string fileId)
        {
            var metadata = await GetFileMetadataAsync(fileId);
            if (metadata == null)
            {
                return; // Already deleted or not found
            }
            
            var filePath = GetFilePath(fileId, metadata.FileName, metadata.Folder);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            
            // Delete metadata
            var metadataPath = GetMetadataPath(fileId);
            if (File.Exists(metadataPath))
            {
                File.Delete(metadataPath);
            }
        }

        public async Task<IEnumerable<FileMetadata>> ListFilesAsync(string folder = null)
        {
            var metadataFiles = Directory.GetFiles(
                Path.Combine(_basePath, ".metadata"),
                "*.json",
                SearchOption.TopDirectoryOnly);
                
            var result = new List<FileMetadata>();
            
            foreach (var metadataFile in metadataFiles)
            {
                var metadata = await LoadMetadataAsync(Path.GetFileNameWithoutExtension(metadataFile));
                if (metadata != null && (string.IsNullOrEmpty(folder) || metadata.Folder == folder))
                {
                    result.Add(metadata);
                }
            }
            
            return result.OrderByDescending(m => m.UploadDate);
        }

        public async Task<FileMetadata> GetFileMetadataAsync(string fileId)
        {
            return await LoadMetadataAsync(fileId);
        }

        public string GetFileUrl(string fileId)
        {
            return $"{_baseUrl}/files/{fileId}";
        }

        // Helper methods
        private string SanitizeFileName(string fileName)
        {
            return Path.GetInvalidFileNameChars()
                .Aggregate(fileName, (current, c) => current.Replace(c, '_'));
        }

        private string GetFilePath(string fileId, string fileName, string folder)
        {
            var targetFolder = string.IsNullOrEmpty(folder) ? _basePath : Path.Combine(_basePath, folder);
            return Path.Combine(targetFolder, $"{fileId}_{fileName}");
        }

        private string GetMetadataPath(string fileId)
        {
            var metadataFolder = Path.Combine(_basePath, ".metadata");
            if (!Directory.Exists(metadataFolder))
            {
                Directory.CreateDirectory(metadataFolder);
            }
            
            return Path.Combine(metadataFolder, $"{fileId}.json");
        }

        private async Task SaveMetadataAsync(FileMetadata metadata)
        {
            var metadataPath = GetMetadataPath(metadata.Id);
            var json = System.Text.Json.JsonSerializer.Serialize(metadata);
            await File.WriteAllTextAsync(metadataPath, json);
        }

        private async Task<FileMetadata> LoadMetadataAsync(string fileId)
        {
            var metadataPath = GetMetadataPath(fileId);
            if (!File.Exists(metadataPath))
            {
                return null;
            }
            
            var json = await File.ReadAllTextAsync(metadataPath);
            return System.Text.Json.JsonSerializer.Deserialize<FileMetadata>(json);
        }
    }
}
