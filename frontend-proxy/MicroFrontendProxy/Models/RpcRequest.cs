using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MicroFrontendProxy.Models
{
    public class RpcRequest
    {
        [JsonPropertyName("service")]
        public string Service { get; set; }
        [JsonPropertyName("interface")]
        public string Interface { get; set; }
        [JsonPropertyName("method")]
        public string Method { get; set; }
        [JsonPropertyName("headers")]
        public Dictionary<string, string> Headers { get; set; }
        [JsonPropertyName("message")]
        public JsonElement Message { get; set; }
    }
}
