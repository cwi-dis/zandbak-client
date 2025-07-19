using System.Collections.Generic;

namespace Orchestrator.Data
{
    [System.Serializable]
    public class AvatarMovementData
    {
        public string userId;
        public Dictionary<string, BoneData> bones;
        public float timestamp;
    }

    [System.Serializable]
    public class MovementData
    {
        public string userId;
        public PositionData position;
        public RotationData rotation;
        public float timestamp;
    }

    [System.Serializable]
    public class PositionData
    {
        public float x;
        public float y;
        public float z;
    }

    [System.Serializable]
    public class RotationData
    {
        public float x;
        public float y;
        public float z;
        public float w;
    }

    [System.Serializable]
    public class BoneData {
        public PositionData pos;
        public RotationData rot;
    }
}
