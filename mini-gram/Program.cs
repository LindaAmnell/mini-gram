
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

builder.Configuration.AddAzureKeyVault(
    keyvaulturi,
    credential
);


var blobConnString = builder.Configuration["blobkey"];

builder.Services.AddSingleton(x =>
    new BlobServiceClient(blobConnString));

builder.Services.AddSingleton<BlobStorageService>();
builder.Services.AddSingleton<BildService>();

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

app.MapGet("/bilder", async (BildService bildService) =>
{
    var bilder = await bildService.HamtaAllaAsync();

    return Results.Ok(bilder);
})
.WithName("HamtaBilder")
.WithSummary("Hämta alla bilder — alla roller");

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

app.MapGet("/roll", (HttpRequest req) =>
{
    var roll = HamtaRoll(req);

    return Results.Ok(new { roll });
})
.WithName("HamtaRoll")
.WithSummary("Hämtar aktuell användarroll");

app.Run();

string HamtaRoll(HttpRequest request)
{
    var header = request.Headers["X-MS-CLIENT-PRINCIPAL"].FirstOrDefault();
    if (string.IsNullOrEmpty(header)) return "Betraktare"; // lokal dev

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

    return "Betraktare"; 
}

bool HarBehorighet(string roll, string kravRoll) => (roll, kravRoll) switch
{
    (_, "Betraktare") => true,
    ("Fotograf" or "Admin", "Fotograf") => true,
    ("Admin", "Admin") => true,
    _ => false
};
