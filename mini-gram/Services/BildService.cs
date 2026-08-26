using mini_gram.Models;

namespace mini_gram.Services;

public class BildService
{
    private readonly BlobStorageService _blobStorageService;

    private readonly List<Bild> _bilder =
    [
        new Bild(
            1,
            "demo.jpg",
            "Demobild — ersätt med din egen",
            ["demo", "placeholder"],
            "https://placehold.co/400x300?text=MinGram"
        )
    ];

    private int _nastaBildId = 2;

    public BildService(BlobStorageService blobStorageService)
    {
        _blobStorageService = blobStorageService;
    }

    // Hämta alla bilder
    public List<Bild> HamtaAlla()
    {
        return _bilder;
    }

    // Hämta en bild
    public Bild? HamtaMedId(int id)
    {
        return _bilder.FirstOrDefault(b => b.Id == id);
    }

    // Ladda upp och skapa en bild
    public async Task<Bild> SkapaBildAsync(
        IFormFile fil,
        string caption,
        string? taggar)
    {
        using var stream = fil.OpenReadStream();

        var url = await _blobStorageService.UploadAsync(
            fil.FileName,
            stream,
            fil.ContentType
        );

        var taggLista = string.IsNullOrWhiteSpace(taggar)
            ? new List<string>()
            : taggar
                .Split(",")
                .Select(t => t.Trim())
                .ToList();

        var bild = new Bild(
            _nastaBildId++,
            fil.FileName,
            caption,
            taggLista,
            url
        );

        _bilder.Add(bild);

        return bild;
    }

    // Uppdatera caption och taggar
    public Bild? UppdateraBild(int id, BildUpdate update)
    {
        var index = _bilder.FindIndex(b => b.Id == id);

        if (index < 0)
            return null;

        _bilder[index] = _bilder[index] with
        {
            Caption = update.Caption ?? _bilder[index].Caption,
            Taggar = update.Taggar ?? _bilder[index].Taggar
        };

        return _bilder[index];
    }

    // Ta bort bild
    public async Task<bool> RaderaBildAsync(int id)
    {
        var bild = _bilder.FirstOrDefault(b => b.Id == id);

        if (bild is null)
            return false;

        await _blobStorageService.DeleteAsync(bild.Namn);

        _bilder.Remove(bild);

        return true;
    }
}