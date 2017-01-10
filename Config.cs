using Newtonsoft.Json;

namespace Screenshotter
{
    public class Config
    {
        [JsonProperty("consumerKey")]
        public string ConsumerKey { get; set; }

        [JsonProperty("consumerSecret")]
        public string ConsumerSecret { get; set; }

        [JsonProperty("accessToken")]
        public string AccessToken { get; set; }

        [JsonProperty("accessSecret")]
        public string AccessSecret { get; set; }
    }
}