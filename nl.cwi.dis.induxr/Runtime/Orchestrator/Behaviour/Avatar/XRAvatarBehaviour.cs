using System.Collections.Generic;
using Orchestrator.Data;
using UnityEngine;

namespace Orchestrator.Behaviour.Avatar
{
    /// <summary>
    /// Avatar behaviour specialized for XR Origins.
    /// Synchronizes the root position, head (camera), and both hands.
    /// </summary>
    public class XRAvatarBehaviour : AvatarBehaviour
    {
        [Header("XR Components")]
        [Tooltip("The transform representing the player's head (usually the Main Camera).")]
        public Transform headTransform;
        [Tooltip("The transform representing the player's left hand.")]
        public Transform leftHandTransform;
        [Tooltip("The transform representing the player's right hand.")]
        public Transform rightHandTransform;

        private const string RootKey = "root";
        private const string HeadKey = "head";
        private const string LeftHandKey = "leftHand";
        private const string RightHandKey = "rightHand";

        // Snapshot storage for interpolation
        private Vector3 _rootInterpStartPos;
        private Quaternion _rootInterpStartRot;

        private Vector3 _headInterpStartPos;
        private Quaternion _headInterpStartRot;

        private Vector3 _leftHandInterpStartPos;
        private Quaternion _leftHandInterpStartRot;

        private Vector3 _rightHandInterpStartPos;
        private Quaternion _rightHandInterpStartRot;

        protected override void InterpolatePose()
        {
            if (LastReceivedData == null) return;

            float t = Mathf.Clamp01((Time.realtimeSinceStartup - LastReceivedTime) * linearInterpolationRate);

            // Interpolate Root
            InterpolateTransform(transform, RootKey, _rootInterpStartPos, _rootInterpStartRot, t);

            // Interpolate Head
            if (headTransform)
                InterpolateTransform(headTransform, HeadKey, _headInterpStartPos, _headInterpStartRot, t);

            // Interpolate Left Hand
            if (leftHandTransform)
                InterpolateTransform(leftHandTransform, LeftHandKey, _leftHandInterpStartPos, _leftHandInterpStartRot, t);

            // Interpolate Right Hand
            if (rightHandTransform)
                InterpolateTransform(rightHandTransform, RightHandKey, _rightHandInterpStartPos, _rightHandInterpStartRot, t);
        }

        private void InterpolateTransform(Transform target, string key, Vector3 startPos, Quaternion startRot, float t)
        {
            if (LastReceivedData.Transforms.TryGetValue(key, out var data))
            {
                target.position = Vector3.Lerp(startPos, data.Pos.ToVector3(), t);
                target.rotation = Quaternion.Slerp(startRot, data.Rot.ToQuaternion(), t);
            }
        }

        protected override AvatarPoseData CollectPoseData()
        {
            var transforms = new Dictionary<string, BoneData>();

            // Root
            transforms.Add(RootKey, CreateBoneData(transform));

            // Head
            if (headTransform)
                transforms.Add(HeadKey, CreateBoneData(headTransform));

            // Hands
            if (leftHandTransform)
                transforms.Add(LeftHandKey, CreateBoneData(leftHandTransform));

            if (rightHandTransform)
                transforms.Add(RightHandKey, CreateBoneData(rightHandTransform));

            return new AvatarPoseData
            {
                UserId = User.Id,
                Timestamp = Time.time,
                Transforms = transforms
            };
        }

        private BoneData CreateBoneData(Transform t)
        {
            return new BoneData
            {
                Pos = new PositionData { X = t.position.x, Y = t.position.y, Z = t.position.z },
                Rot = new RotationData { X = t.rotation.x, Y = t.rotation.y, Z = t.rotation.z, W = t.rotation.w }
            };
        }

        protected override void OnPoseReceived(AvatarPoseData pose)
        {
            // Snapshot current local states for smooth interpolation
            _rootInterpStartPos = transform.position;
            _rootInterpStartRot = transform.rotation;

            if (headTransform)
            {
                _headInterpStartPos = headTransform.position;
                _headInterpStartRot = headTransform.rotation;
            }

            if (leftHandTransform)
            {
                _leftHandInterpStartPos = leftHandTransform.position;
                _leftHandInterpStartRot = leftHandTransform.rotation;
            }

            if (rightHandTransform)
            {
                _rightHandInterpStartPos = rightHandTransform.position;
                _rightHandInterpStartRot = rightHandTransform.rotation;
            }

            base.OnPoseReceived(pose);
        }

        protected override void SetPose(AvatarPoseData pose)
        {
            ApplyTransformData(transform, RootKey, pose);

            if (headTransform)
                ApplyTransformData(headTransform, HeadKey, pose);

            if (leftHandTransform)
                ApplyTransformData(leftHandTransform, LeftHandKey, pose);

            if (rightHandTransform)
                ApplyTransformData(rightHandTransform, RightHandKey, pose);
        }

        private void ApplyTransformData(Transform target, string key, AvatarPoseData pose)
        {
            if (pose.Transforms.TryGetValue(key, out var data))
            {
                target.position = data.Pos.ToVector3();
                target.rotation = data.Rot.ToQuaternion();
            }
        }
    }

    internal static class BoneDataExtensions
    {
        public static Vector3 ToVector3(this PositionData p) => new Vector3(p.X, p.Y, p.Z);
        public static Quaternion ToQuaternion(this RotationData r) => new Quaternion(r.X, r.Y, r.Z, r.W);
    }
}
