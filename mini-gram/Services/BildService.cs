using mini_gram.Models;

namespace mini_gram.Services;

public class BildService
{
    private readonly BlobStorageService _blobStorageService;

    public BildService(BlobStorageService blobStorageService)
    {
        _blobStorageService = blobStorageService;
    }

    public async Task<List<Bild>> HamtaAllaAsync()
    {
        return await _blobStorageService.HamtaAllaAsync();
    }


    public async Task<Bild?> HamtaEnAsync(string namn)
    {
        return await _blobStorageService.HamtaEnAsync(namn);
    }

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

        var uniqueFileName = await _blobStorageService.UploadAsync(
        fil.FileName,
        stream,
        fil.ContentType,
        caption,
        taggLista
        );

        var bild = await _blobStorageService.HamtaEnAsync(uniqueFileName);

        return bild!;
    }
    public async Task<Bild?> UppdateraBildAsync(
        string namn,
        BildUpdate update)
    {
        return await _blobStorageService
            .UppdateraMetadataAsync(namn, update);
    }
    public async Task<bool> RaderaBildAsync(string namn)
    {
        return await _blobStorageService.DeleteAsync(namn);
    }
}