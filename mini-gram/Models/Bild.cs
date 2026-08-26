namespace mini_gram.Models
{
    public record Bild(
    int Id,
    string Namn,
    string Caption,
    List<string> Taggar,
    string Url
);
}
