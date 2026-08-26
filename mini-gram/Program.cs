// Program.cs — MinGram API
// ASP.NET Core Minimal API: endpoints definieras direkt här, inga controllers.
//
// Starta lokalt:  dotnet run
// Swagger UI:     https://localhost:{port}/swagger
//
// v35 — Azure-konfiguration (görs i portalen, inte i koden):
// 1. CORS: App Service → API → CORS → lägg till din frontend-URL
// 2. Easy Auth: App Service → Authentication → Add identity provider → Microsoft
//    Välj din Entra ID-tenant. Alla anrop kräver nu inloggning.
// 3. App-roller i Entra ID: gå till App registrations → din app → App roles
//    Skapa rollerna Betraktare, Fotograf, Admin.
//    Tilldela dem till dina Entra ID-användare under Enterprise applications.
//
// Bilder lagras i Azure Blob Storage.
// Caption och taggar sparas som metadata på blobben.

using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using mini_gram.Models;
using mini_gram.Services;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

// ======================================================
// Key Vault
// ======================================================

var keyVaultUrl = builder.Configuration["keyVaultURL"];
var keyvaulturi = new Uri(keyVaultUrl!);

TokenCredential credential;

if (builder.Environment.IsDevelopment())
{
    credential = new AzureCliCredential();
}
else
{
    credential = new DefaultAzureCredential();
}

Console.WriteLine("FÖRE KEY VAULT");

builder.Configuration.AddAzureKeyVault(
    keyvaulturi,
    credential
);

Console.WriteLine("EFTER KEY VAULT");


// ======================================================
// Blob Storage
// ======================================================

var blobConnString = builder.Configuration["blobkey"];

builder.Services.AddSingleton(x =>
    new BlobServiceClient(blobConnString));

builder.Services.AddSingleton<BlobStorageService>();
builder.Services.AddSingleton<BildService>();

// CORS — hanteras primärt i Azure Portal: App Service → API → CORS
// Lägg till din frontend-URL där, så slipper du ändra och redeploya koden.
// Den här koden hanterar CORS lokalt under utveckling.

builder.Services.AddCors(options =>
{
    options.AddPolicy("MinGramPolicy", policy =>
    {
        var origins = builder.Configuration
                             .GetSection("AllowedOrigins")
                             .Get<string[]>() ?? [];
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("MinGramPolicy");

// ======================================================
// Bilder
// ======================================================

// Alla roller får se bilder
app.MapGet("/bilder", async (BildService bildService) =>
{
    var bilder = await bildService.HamtaAllaAsync();

    return Results.Ok(bilder);
})
.WithName("HamtaBilder")
.WithSummary("Hämta alla bilder — alla roller");


// Alla roller får hämta en specifik bild
app.MapGet("/bilder/{namn}", async (
    string namn,
    BildService bildService) =>
{
    var bild = await bildService.HamtaEnAsync(namn);

    return bild is not null
        ? Results.Ok(bild)
        : Results.NotFound();
})
.WithName("HamtaBild")
.WithSummary("Hämta en specifik bild — alla roller");

// Fotograf och Admin får ladda upp bilder
// Filen laddas upp till Azure Blob Storage.
// URL:en till blobben sparas sedan i Bild-objektet.
app.MapPost("/bilder", async (
    IFormFile fil,
    string caption,
    string? taggar,
    HttpRequest req,
    BildService bildService) =>
{
    if (!HarBehorighet(HamtaRoll(req), "Fotograf"))
        return Results.StatusCode(403);

    var bild = await bildService.SkapaBildAsync(
        fil,
        caption,
        taggar
    );

    return Results.Created($"/bilder/{bild.Namn}", bild);
})
.DisableAntiforgery()
.WithName("LaddaUppBild")
.WithSummary("Ladda upp bild — kräver Fotograf eller Admin");


// Fotograf och Admin får uppdatera caption och taggar
app.MapPut("/bilder/{namn}", async (
    string namn,
    BildUpdate update,
    HttpRequest req,
    BildService bildService) =>
{
    if (!HarBehorighet(HamtaRoll(req), "Fotograf"))
        return Results.StatusCode(403);

    var bild = await bildService.UppdateraBildAsync(namn, update);

    return bild is not null
        ? Results.Ok(bild)
        : Results.NotFound();
})
.WithName("UppdateraBild")
.WithSummary("Uppdatera bild — kräver Fotograf eller Admin");

// Bara Admin får ta bort bilder — testa med Postman som Betraktare för att se 403
app.MapDelete("/bilder/{namn}", async (
    string namn,
    HttpRequest req,
    BildService bildService) =>
{
    if (!HarBehorighet(HamtaRoll(req), "Admin"))
        return Results.StatusCode(403);

    var borttagen = await bildService.RaderaBildAsync(namn);

    return borttagen
        ? Results.NoContent()
        : Results.NotFound();
})
.WithName("RaderaBild")
.WithSummary("Radera bild — kräver Admin");


app.Run();

// ======================================================
// Rollkontroll
// ======================================================

// Läser rollen ur Easy Auth-headern som Azure injicerar efter inloggning.
// Lokalt (utan Easy Auth): returnerar "Admin" så Swagger fungerar utan inloggning.
string HamtaRoll(HttpRequest request)
{
    var header = request.Headers["X-MS-CLIENT-PRINCIPAL"].FirstOrDefault();
    if (string.IsNullOrEmpty(header)) return "Admin"; // lokal dev

    try
    {
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(header));
        using var doc = JsonDocument.Parse(json);
        foreach (var claim in doc.RootElement.GetProperty("claims").EnumerateArray())
        {
            if (claim.GetProperty("typ").GetString() == "roles")
                return claim.GetProperty("val").GetString() ?? "Betraktare";
        }
    }
    catch { }

    return "Betraktare"; // okänd roll → minsta behörighet
}

// Kontrollerar om en roll har tillräcklig behörighet.
// Hierarki: Betraktare < Fotograf < Admin
bool HarBehorighet(string roll, string kravRoll) => (roll, kravRoll) switch
{
    (_, "Betraktare") => true,
    ("Fotograf" or "Admin", "Fotograf") => true,
    ("Admin", "Admin") => true,
    _ => false
};
