using System;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using Orchestrator.Data;

namespace Orchestrator.Behaviour
{
    [Obsolete]
    public class AvatarNetworkBehaviour : NetworkBehaviour
    {
        private SkinnedMeshRenderer _mesh;
        private Rigidbody _parentRigidbody;

        // Use this for initialization
        void Start()
        {
            Initialize();
            _mesh = GetComponent<SkinnedMeshRenderer>();

            if (!isLocal) {
                _parentRigidbody = GetComponentInParent<Rigidbody>();

                if (_parentRigidbody) {
                    _parentRigidbody.isKinematic = true;
                }
            }
        }

        public override object SendPositionData()
        {
            var boneData = new Dictionary<string, BoneData>();

            foreach (var bone in _mesh.bones) {
                boneData.Add(bone.name, new BoneData {
                    Pos = new PositionData
                    {
                        X = bone.position.x,
                        Y = bone.position.y,
                        Z = bone.position.z
                    },
                    Rot = new RotationData
                    {
                        X = bone.rotation.x,
                        Y = bone.rotation.y,
                        Z = bone.rotation.z,
                        W = bone.rotation.w
                    },
                });
            }

            return new AvatarMovementData {
                UserId = id,
                Timestamp = Time.time,
                Bones = boneData
            };
        }

        public override void OnBroadcastReceived(BroadcastData data)
        {
            if (data.Channel == "transform")
            {
                var movement = JsonConvert.DeserializeObject<AvatarMovementData>(data.Data);

                if (movement.UserId == id) {
                    foreach (var bone in _mesh.bones) {
                        if (movement.Bones.TryGetValue(bone.name, out var foundBone)) {
                            bone.SetPositionAndRotation(new Vector3(
                                foundBone.Pos.X,
                                foundBone.Pos.Y,
                                foundBone.Pos.Z
                            ), new Quaternion(
                                foundBone.Rot.X,
                                foundBone.Rot.Y,
                                foundBone.Rot.Z,
                                foundBone.Rot.W
                            ));
                        }
                    }
                }
            }
        }
    }
}
