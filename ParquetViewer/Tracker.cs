using System.Text;
using System.Text.Json;

namespace LP.Domain {

    /// <summary>
    /// Simple stats tracker (no personal data allowed)
    /// </summary>
    public class Tracker {
        private readonly string _url;
        private readonly HttpClient _httpClient;
        private readonly string _version;

        public static Tracker Instance { get; set; }

        public Tracker(string key, string version, string url = "https://alt.aloneguid.uk/events") {
            _url = $"{url}?key={key}";
            _httpClient = new HttpClient();
            _version = version;
        }

        public async ValueTask Track(string eventName, Dictionary<string, string>? extras = null) {
            if(extras == null)
                extras = new Dictionary<string, string>();
            var request = new HttpRequestMessage(HttpMethod.Post, _url);
            extras["t"] = DateTime.UtcNow.ToString("o");
            extras["version"] = _version;
            extras["e"] = eventName;
            request.Content = new StringContent(
               JsonSerializer.Serialize(
                  extras
                     .Where(e => !string.IsNullOrEmpty(e.Key) && !string.IsNullOrEmpty(e.Value))
                     .ToDictionary(e => e.Key, v => v.Value)
                     ),
               Encoding.UTF8, "application/json");
            HttpResponseMessage? response = await _httpClient.SendAsync(request);
            response?.EnsureSuccessStatusCode();
        }
    }
}