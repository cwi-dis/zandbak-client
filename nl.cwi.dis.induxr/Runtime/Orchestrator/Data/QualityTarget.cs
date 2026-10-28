using Newtonsoft.Json;

namespace Orchestrator.Data
{
    public class QualityTarget : IOrchestratorElement
    {
        [JsonProperty("poseUpdateRate")] public float? PoseUpdateRate;
        [JsonProperty("voiceBitrate")] public int? VoiceBitrate;
        [JsonProperty("interpolationBufferMs")] public int? InterpolationBufferMs;
    }
}
