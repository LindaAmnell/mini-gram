using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace mini_gram.Services
{
    public class BlobStorageService
    {
        private readonly BlobContainerClient _container;

        public BlobStorageService(BlobServiceClient blobServiceClient)
        {
            _container = blobServiceClient.GetBlobContainerClient("mini-gram-bilder");
        }

        public async Task<string> UploadAsync(
            string fileName,
            Stream stream,
            string contentType)
        {
            await _container.CreateIfNotExistsAsync();

            var blobClient = _container.GetBlobClient(fileName);

            var options = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                }
            };

            await blobClient.UploadAsync(stream, options);

            return blobClient.Uri.ToString();
        }

        public async Task DeleteAsync(string fileName)
        {
            var blobClient = _container.GetBlobClient(fileName);

            await blobClient.DeleteIfExistsAsync();
        }
    }
}
