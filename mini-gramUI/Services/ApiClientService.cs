using System.Text;

namespace mini_gramUI.Services
{
    public class ApiClientService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ApiClientService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public HttpClient SkapaClientMedRoll(string roll)
        {
            var client = _httpClientFactory.CreateClient("MinGramApi");

            var json =
                $"{{\"claims\":[{{\"typ\":\"roles\",\"val\":\"{roll}\"}}]}}";

            var base64 = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(json)
            );

            client.DefaultRequestHeaders.Remove(
                "X-MS-CLIENT-PRINCIPAL"
            );

            client.DefaultRequestHeaders.Add(
                "X-MS-CLIENT-PRINCIPAL",
                base64
            );

            return client;
        }
    }
}