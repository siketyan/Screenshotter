using Newtonsoft.Json;

namespace Screenshotter
{
    public class Credentials
    {
        [JsonProperty("accessToken")]
        public string AccessToken { get; set; }

        [JsonProperty("accessSecret")]
        public string AccessSecret { get; set; }
    }
}