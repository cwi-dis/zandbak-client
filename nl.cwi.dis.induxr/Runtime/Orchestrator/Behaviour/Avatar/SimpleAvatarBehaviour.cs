using System.Collections.Generic;
using Orchestrator.App;
using Orchestrator.Data;
using UnityEngine;

namespace Orchestrator.Behaviour.Avatar
{
    public class SimpleAvatarBehaviour : AvatarBehaviour
    {
        private const string TransformBoneName = "root";

        private Vector3 _interpStartPos;
        private Quaternion _interpStartRot;

        protected override void InterpolatePose()
        {
            if (LastReceivedData == null)
                return;

            var t = Mathf.Clamp01((Time.realtimeSinceStartup - LastReceivedTime) / (1.0f / linearInterpolationRate));

            if (LastReceivedData.Transforms.TryGetValue(TransformBoneName, out var lastFoundBone))
            {
                transform.SetPositionAndRotation(
                    Vector3.Lerp(
                        _interpStartPos,
                        new Vector3(lastFoundBone.Pos.X, lastFoundBone.Pos.Y, lastFoundBone.Pos.Z),
                        t
                    ),
                    Quaternion.Slerp(
                        _interpStartRot,
                        new Quaternion(lastFoundBone.Rot.X, lastFoundBone.Rot.Y, lastFoundBone.Rot.Z, lastFoundBone.Rot.W),
                        t
                    )
                );
            }
        }

        protected override AvatarPoseData CollectPoseData()
        {
            var positionData = new Dictionary<string, BoneData>
            {
                {
                    TransformBoneName,
                    new BoneData
                    {
                        Pos = new PositionData { X = transform.position.x, Y = transform.position.y, Z = transform.position.z },
                        Rot = new RotationData { X = transform.rotation.x, Y = transform.rotation.y, Z = transform.rotation.z, W = transform.rotation.w }
                    }
                }
            };

            return new AvatarPoseData {
                UserId = User.Id,
                Timestamp = Time.time,
                Transforms = positionData
            };
        }

        protected override void OnPoseReceived(AvatarPoseData pose)
        {
            _interpStartPos = transform.position;
            _interpStartRot = transform.rotation;
            base.OnPoseReceived(pose);
        }

        protected override void SetPose(AvatarPoseData pose)
        {
            if (pose.Transforms.TryGetValue(TransformBoneName, out var foundBone)) {
                transform.SetPositionAndRotation(new Vector3(
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
