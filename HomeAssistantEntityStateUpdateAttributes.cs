using Newtonsoft.Json;

namespace TeamsPresence
{
    public class HomeAssistantEntityStateUpdateAttributes
    {
        [JsonProperty(PropertyName = "friendly_name")]
        public string FriendlyName { get; set; }
        [JsonProperty(PropertyName = "state")]
        public string state { get; set; }
        [JsonProperty(PropertyName = "icon")]
        public string Icon { get; set; }
        [JsonProperty(PropertyName = "device_class")]
        public string DeviceClass { get; set; }
        [JsonProperty(PropertyName = "options")]
        public string[] Options { get; set; }
    }
}
