using System.Collections.Generic;
using Newtonsoft.Json;

namespace Orchestrator.Data
{
    public class AvatarMovementData
    {
        [JsonProperty("userId")] public string UserId;
        [JsonProperty("bones")] public Dictionary<string, BoneData> Bones;
        [JsonProperty("timestamp")] public float Timestamp;
    }

    public class MovementData
    {
        [JsonProperty("userId")] public string UserId;
        [JsonProperty("position")] public PositionData Position;
        [JsonProperty("rotation")] public RotationData Rotation;
        [JsonProperty("timestamp")] public float Timestamp;
    }

    public class PositionData
    {
        [JsonProperty("x")] public float X;
        [JsonProperty("y")] public float Y;
        [JsonProperty("z")] public float Z;
    }

    public class RotationData
    {
        [JsonProperty("x")] public float X;
        [JsonProperty("y")] public float Y;
        [JsonProperty("z")] public float Z;
        [JsonProperty("w")] public float W;
    }

    public class BoneData {
        [JsonProperty("pos")] public PositionData Pos;
        [JsonProperty("rot")] public RotationData Rot;
    }
}
