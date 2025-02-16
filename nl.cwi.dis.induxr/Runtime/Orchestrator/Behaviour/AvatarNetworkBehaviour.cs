using UnityEngine;
using Orchestrator.Responses;

namespace Orchestrator.Behaviours
{
    [System.Serializable]
    public class AvatarMovementData
    {
        public string userId;
        public BoneData[] bones;
        public float timestamp;
    }

    [System.Serializable]
    public class BoneData {
        public string name;
        public PositionData position;
        public RotationData rotation;
    }

    public class AvatarNetworkBehaviour : NetworkBehaviour
    {
        private SkinnedMeshRenderer mesh;

        // Use this for initialization
        void Start()
        {
            Initialize();
            mesh = GetComponent<SkinnedMeshRenderer>();
        }

        public override object SendPositionData()
        {
            var boneData = new BoneData[mesh.bones.Length];
            int i = 0;

            foreach (var bone in mesh.bones) {
                boneData[i] = new BoneData {
                    name = bone.name,
                    position = new PositionData {
                        x = bone.position.x,
                        y = bone.position.y,
                        z = bone.position.z
                    },
                    rotation = new RotationData {
                        x = bone.rotation.x,
                        y = bone.rotation.y,
                        z = bone.rotation.z,
                        w = bone.rotation.w
                    },
                };

                i++;
            }

            return new AvatarMovementData {
                userId = Id,
                timestamp = Time.time,
                bones = boneData
            };
        }

        public override void OnBroadcastReceived(BroadcastData data)
        {
            Debug.Log(data.channel + " " + data.data);
        }
    }
}
