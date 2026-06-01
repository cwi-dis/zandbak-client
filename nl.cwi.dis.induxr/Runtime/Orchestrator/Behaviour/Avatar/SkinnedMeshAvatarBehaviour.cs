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
        [Header("General Options")]
        [Tooltip("A reference to the object to be used as notification icon. If given, the notification icon will be shown when the user's hand is raised.")]
        public GameObject notification;
        [Tooltip("A reference to a text object to be used for displaying the user's username.")]
        public TextMesh usernamePlaque;

        [Header("Local Options")]
        [Tooltip("How many times a second the pose data should be broadcast to the server.")]
        public int updateRate = 10;

        [Header("Remote Options (Smoothing)")]
        public int linearInterpolationRate = 5;

        private User _user;
        private bool _isLocal;
        private SkinnedMeshRenderer _mesh;

        // Local state
        private float _updateTimer;

        // Remote state
        private AvatarMovementData _previousReceivedData;

        private AvatarMovementData _lastReceivedData;
        private float _lastReceiveTime;
        private Dictionary<string, Vector3> _interpolationBonePosition = new();
        private Dictionary<string, Quaternion> _interpolationBoneRotation = new();


        public override void Initialize(User user)
        {
            _user = user;
            _user.Avatar = gameObject;
            _isLocal = user is SelfUser;
        }

        private void Start()
        {
            if (_user == null)
            {
                Debug.LogError("User is null. Make sure to call Initialize()");
                return;
            }

            _mesh = GetComponentInChildren<SkinnedMeshRenderer>();

            if (!_mesh)
            {
                Debug.LogError("SkinnedMeshRenderer component not found on avatar game object");
                return;
            }

            if (usernamePlaque != null)
            {
                usernamePlaque.text = _user.Name;
            }

            if (notification != null)
            {
                _user.OnHandRaised += (isRaised) => notification.SetActive(isRaised);
            }

            if (_isLocal)
            {
                // hide username plaque for the local user
                Debug.Log("Hiding username plaque for local user");
                usernamePlaque?.gameObject.SetActive(false);
            }
            else
            {
                _lastReceiveTime = Time.realtimeSinceStartup;

                if (_user.Transform != null)
                {
                    UpdatePose(_user.Transform);
                }

                _user.OnAvatarMovementReceived += PoseReceived;
                _user.OnIsSpeaking += (isSpeaking) => Debug.Log($"{_user.Name} is speaking: {isSpeaking}");

                // Disable all other behaviours for remote avatars
                foreach (var comp in GetComponents<UnityEngine.Behaviour>())
                {
                    if (comp != this) comp.enabled = false;
                }
            }
        }

        private void Update()
        {
            if (_user == null) return;
            if (!_mesh) return;

            if (_isLocal)
            {
                BroadcastPose();
            }
            else
            {
                ApplyPose();
            }
        }

        private void BroadcastPose()
        {
            _updateTimer += Time.deltaTime;

            if (_updateTimer >= 1f / updateRate)
            {
                _updateTimer -= 1f / updateRate;
                var data = GetBoneData();
                ((SelfUser)_user).BroadcastAvatarMovement(data);
            }
        }

        private void ApplyPose()
        {
            if (_lastReceivedData == null)
                return;

            var t = Mathf.Clamp01((Time.realtimeSinceStartup - _lastReceiveTime) * linearInterpolationRate);

            foreach (var bone in _mesh.bones)
            {
                if (_lastReceivedData.Transforms.TryGetValue(bone.name, out var lastFoundBone) && _interpolationBonePosition.TryGetValue(bone.name, out var currentBonePosition) && _interpolationBoneRotation.TryGetValue(bone.name, out var currentBoneRotation))
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

        private AvatarMovementData GetBoneData()
        {
            var boneData = _mesh.bones.ToDictionary(bone => bone.name, bone => new BoneData
            {
                Pos = new PositionData { X = bone.position.x, Y = bone.position.y, Z = bone.position.z },
                Rot = new RotationData { X = bone.rotation.x, Y = bone.rotation.y, Z = bone.rotation.z, W = bone.rotation.w },
            });

            return new AvatarMovementData {
                UserId = _user.Id,
                Timestamp = Time.time,
                Transforms = boneData
            };
        }

        private void PoseReceived(AvatarMovementData movement)
        {
            _lastReceivedData = movement;
            _lastReceiveTime = Time.realtimeSinceStartup;

            foreach (var bone in _mesh.bones)
            {
                // Snapshot current position and rotation for the bone
                _interpolationBonePosition[bone.name] = bone.position;
                _interpolationBoneRotation[bone.name] = bone.rotation;
            }
        }

        private void UpdatePose(AvatarMovementData movement)
        {
            foreach (var bone in _mesh.bones)
            {
                if (movement.Transforms.TryGetValue(bone.name, out var foundBone)) {
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
