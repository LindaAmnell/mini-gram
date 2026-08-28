using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using mini_gram.Models;

namespace mini_gram.Services
{
    public class BlobStorageService
    {
        private readonly BlobContainerClient _container;

        public BlobStorageService(BlobServiceClient blobServiceClient)
        {
            _container = blobServiceClient
                .GetBlobContainerClient("mini-gram-bilder");
        }
        private string SkapaSasUrl(BlobClient blobClient)
        {
            if (!blobClient.CanGenerateSasUri)
            {
                return blobClient.Uri.ToString();
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = blobClient.BlobContainerName,
                BlobName = blobClient.Name,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(30)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return blobClient.GenerateSasUri(sasBuilder).ToString();
        }

        public async Task<string> UploadAsync(
            string fileName,
            Stream stream,
            string contentType,
            string caption,
            List<string> taggar)
        {
            await _container.CreateIfNotExistsAsync();

            var uniqueFileName = $"{Guid.NewGuid()}-{fileName}";
            var blobClient = _container.GetBlobClient(uniqueFileName);

            var options = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                },

                Metadata = new Dictionary<string, string>
                {
                    { "caption", caption },
                    { "taggar", string.Join(",", taggar) }
                }
            };

            await blobClient.UploadAsync(stream, options);

            return blobClient.Uri.ToString();
        }

        public async Task<List<Bild>> HamtaAllaAsync()
        {
            var bilder = new List<Bild>();

            var options = new GetBlobsOptions
            {
                Traits = BlobTraits.Metadata
            };

            await foreach (var blob in _container.GetBlobsAsync(options))
            {
                var caption = blob.Metadata.TryGetValue("caption", out var c)
                    ? c
                    : "";

                var taggar = blob.Metadata.TryGetValue("taggar", out var t)
                    ? t.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList()
                    : new List<string>();

                var blobClient = _container.GetBlobClient(blob.Name);

                bilder.Add(new Bild(
                     blob.Name,
                     caption,
                     taggar,
                     blobClient.Uri.ToString(),
                     SkapaSasUrl(blobClient)
                ));
            }

            return bilder;
        }

        public async Task<Bild?> HamtaEnAsync(string fileName)
        {
            var blobClient = _container.GetBlobClient(fileName);

            if (!await blobClient.ExistsAsync())
                return null;

            var properties = await blobClient.GetPropertiesAsync();

            var caption = properties.Value.Metadata.TryGetValue("caption", out var c)
                ? c
                : "";

            var taggar = properties.Value.Metadata.TryGetValue("taggar", out var t)
                ? t.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList()
                : new List<string>();

            return new Bild(
                
                fileName,
                caption,
                taggar,
                blobClient.Uri.ToString(),
                SkapaSasUrl(blobClient)
            );
        }

        public async Task<Bild?> UppdateraMetadataAsync(
            string fileName,
            BildUpdate update)
        {
            var blobClient = _container.GetBlobClient(fileName);

            if (!await blobClient.ExistsAsync())
                return null;

            var properties = await blobClient.GetPropertiesAsync();

            var metadata = properties.Value.Metadata;

            if (update.Caption is not null)
            {
                metadata["caption"] = update.Caption;
            }

            if (update.Taggar is not null)
            {
                metadata["taggar"] = string.Join(",", update.Taggar);
            }

            await blobClient.SetMetadataAsync(metadata);

            return await HamtaEnAsync(fileName);
        }

        public async Task<bool> DeleteAsync(string fileName)
        {
            var result = await _container
                .GetBlobClient(fileName)
                .DeleteIfExistsAsync();

            return result.Value;
        }
    }
}