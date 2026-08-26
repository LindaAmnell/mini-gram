using mini_gram.Models;

namespace mini_gram.Services;

public class BildService
{
    private readonly BlobStorageService _blobStorageService;

    public BildService(BlobStorageService blobStorageService)
    {
        _blobStorageService = blobStorageService;
    }

    // GET alla
    public async Task<List<Bild>> HamtaAllaAsync()
    {
        return await _blobStorageService.HamtaAllaAsync();
    }

    // GET en
    public async Task<Bild?> HamtaEnAsync(string namn)
    {
        return await _blobStorageService.HamtaEnAsync(namn);
    }

    // POST
    public async Task<Bild> SkapaBildAsync(
        IFormFile fil,
        string caption,
        string? taggar)
    {
        var taggLista = string.IsNullOrWhiteSpace(taggar)
            ? new List<string>()
            : taggar
                .Split(",")
                .Select(t => t.Trim())
                .ToList();

        using var stream = fil.OpenReadStream();

        var url = await _blobStorageService.UploadAsync(
            fil.FileName,
            stream,
            fil.ContentType,
            caption,
            taggLista
        );

        return new Bild(
       
            fil.FileName,
            caption,
            taggLista,
            url
        );
    }

    // PUT
    public async Task<Bild?> UppdateraBildAsync(
        string namn,
        BildUpdate update)
    {
        return await _blobStorageService
            .UppdateraMetadataAsync(namn, update);
    }

    // DELETE
    public async Task<bool> RaderaBildAsync(string namn)
    {
        return await _blobStorageService.DeleteAsync(namn);
    }
}