using Newtonsoft.Json;

namespace Orchestrator.Data
{
    public class TelemetryData
    {
        [JsonProperty("rttMs")] public float RttMs;
        [JsonProperty("jitterMs")] public float JitterMs;
        [JsonProperty("packetLossRate")] public float PacketLossRate;
        [JsonProperty("bandwidthKbps")] public float? BandwidthKbps;
        [JsonProperty("timestamp")] public long Timestamp;
    }
}
