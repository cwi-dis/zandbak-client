using System.Collections.Generic;
using System.Linq;
using Orchestrator.Data;
using UnityEngine;
using User = Orchestrator.App.User;
using SelfUser = Orchestrator.App.SelfUser;

namespace Orchestrator.Behaviour.Avatar
{
    public class SkinnedMeshAvatarBehaviour : AvatarBehaviour
    {
        private SkinnedMeshRenderer _mesh;

        private Dictionary<string, Vector3> _interpolationBonePosition = new();
        private Dictionary<string, Quaternion> _interpolationBoneRotation = new();

        protected override void Start()
        {
            _mesh = GetComponentInChildren<SkinnedMeshRenderer>();

            if (!_mesh)
            {
                Debug.LogError("SkinnedMeshRenderer component not found on avatar game object");
                return;
            }

            base.Start();
        }

        protected override void Update()
        {
            if (!_mesh) return;
            base.Update();
        }

        protected override void InterpolatePose()
        {
            if (LastReceivedData == null)
                return;

            var t = Mathf.Clamp01((Time.realtimeSinceStartup - LastReceivedTime) * linearInterpolationRate);

            foreach (var bone in _mesh.bones)
            {
                if (LastReceivedData.Transforms.TryGetValue(bone.name, out var lastFoundBone) && _interpolationBonePosition.TryGetValue(bone.name, out var currentBonePosition) && _interpolationBoneRotation.TryGetValue(bone.name, out var currentBoneRotation))
                {
                    bone.SetPositionAndRotation(
                        Vector3.Lerp(
                            currentBonePosition,
                            new Vector3(lastFoundBone.Pos.X, lastFoundBone.Pos.Y, lastFoundBone.Pos.Z),
                            t
                        ),
                        Quaternion.Slerp(
                            currentBoneRotation,
                            new Quaternion(lastFoundBone.Rot.X, lastFoundBone.Rot.Y, lastFoundBone.Rot.Z, lastFoundBone.Rot.W),
                            t
                        )
                    );
                }
            }
        }

        protected override AvatarPoseData CollectPoseData()
        {
            var boneData = _mesh.bones.ToDictionary(bone => bone.name, bone => new BoneData
            {
                Pos = new PositionData { X = bone.position.x, Y = bone.position.y, Z = bone.position.z },
                Rot = new RotationData { X = bone.rotation.x, Y = bone.rotation.y, Z = bone.rotation.z, W = bone.rotation.w },
            });

            return new AvatarPoseData {
                UserId = User.Id,
                Timestamp = Time.time,
                Transforms = boneData
            };
        }

        protected override void OnPoseReceived(AvatarPoseData pose)
        {
            foreach (var bone in _mesh.bones)
            {
                // Snapshot current position and rotation for the bone
                _interpolationBonePosition[bone.name] = bone.position;
                _interpolationBoneRotation[bone.name] = bone.rotation;
            }
            base.OnPoseReceived(pose);
        }

        protected override void SetPose(AvatarPoseData pose)
        {
            foreach (var bone in _mesh.bones)
            {
                if (pose.Transforms.TryGetValue(bone.name, out var foundBone)) {
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
