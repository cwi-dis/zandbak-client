using UnityEngine;
using System.Collections.Generic;
using Orchestrator.Responses;

namespace Orchestrator.Behaviour
{
    [System.Serializable]
    public class AvatarMovementData
    {
        public string userId;
        public Dictionary<string, BoneData> bones;
        public float timestamp;
    }

    [System.Serializable]
    public class BoneData {
        public PositionData pos;
        public RotationData rot;
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
            var boneData = new Dictionary<string, BoneData>();

            foreach (var bone in mesh.bones) {
                boneData.Add(bone.name, new BoneData {
                    pos = new PositionData
                    {
                        x = bone.position.x,
                        y = bone.position.y,
                        z = bone.position.z
                    },
                    rot = new RotationData
                    {
                        x = bone.rotation.x,
                        y = bone.rotation.y,
                        z = bone.rotation.z,
                        w = bone.rotation.w
                    },
                });
            }

            return new AvatarMovementData {
                userId = Id,
                timestamp = Time.time,
                bones = boneData
            };
        }

        public override void OnBroadcastReceived(BroadcastData data)
        {
            if (data.channel == "transform")
            {
                var movement = JsonUtility.FromJson<AvatarMovementData>(data.data);

                if (movement.userId == Id) {
                    foreach (var bone in mesh.bones) {
                        if (movement.bones.TryGetValue(bone.name, out BoneData foundBone)) {
                            bone.SetPositionAndRotation(new Vector3(
                                foundBone.pos.x,
                                foundBone.pos.y,
                                foundBone.pos.z
                            ), new Quaternion(
                                foundBone.rot.x,
                                foundBone.rot.y,
                                foundBone.rot.z,
                                foundBone.rot.w
                            ));
                        }
                    }
                }
            }
        }
    }
}
