namespace mini_gram.Models
{
    public record Bild(
    string Namn,
    string Caption,
    List<string> Taggar,
    string Url,
    string TidsbegransadUrl
);
}
