namespace mini_gram.Models
{
    public record NyBild(
    string Namn,
    string Caption,
    List<string>? Taggar,
    string Url
);
}
