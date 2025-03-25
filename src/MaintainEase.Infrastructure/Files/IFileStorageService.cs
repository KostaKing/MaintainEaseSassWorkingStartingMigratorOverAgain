using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MaintainEase.Infrastructure.Files
{
    /// <summary>
    /// Interface for file storage service
    /// </summary>
    public interface IFileStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder = null);
        Task<byte[]> DownloadFileAsync(string fileId);
        Task DeleteFileAsync(string fileId);
        Task<IEnumerable<FileMetadata>> ListFilesAsync(string folder = null);
        Task<FileMetadata> GetFileMetadataAsync(string fileId);
        string GetFileUrl(string fileId);
    }

    /// <summary>
    /// File metadata
    /// </summary>
    public class FileMetadata
    {
        public string Id { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long Size { get; set; }
        public DateTimeOffset UploadDate { get; set; }
        public string Folder { get; set; }
        public string Url { get; set; }
    }
}
