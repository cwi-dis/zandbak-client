using System.Linq;
using Orchestrator.Data;
using UnityEngine;
using User = Orchestrator.App.User;
using SelfUser = Orchestrator.App.SelfUser;

namespace Orchestrator.Behaviour.Avatar
{
    public class SkinnedMeshAvatarBehaviour : MonoBehaviour
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
        public bool withSmoothing = false;
        public int linearInterpolationRate = 10;

        private User _user;
        private bool _isLocal;
        private SkinnedMeshRenderer _mesh;

        // Local state
        private float _updateTimer;

        // Remote state
        private AvatarMovementData _previousReceivedData;
        private AvatarMovementData _lastReceivedData;
        private float _lastReceiveTime;

        public void Initialize(User user)
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

            if (!_isLocal)
            {
                _lastReceiveTime = Time.realtimeSinceStartup;

                if (_user.Transform != null)
                {
                    UpdatePose(_user.Transform);
                }

                _user.OnAvatarMovementReceived += PoseReceived;
                _user.OnIsSpeaking += (isSpeaking) => Debug.Log($"{_user.Name} is speaking: {isSpeaking}");

                // Disable all other behaviours for remote avatars
                foreach (var comp in GetComponents<MonoBehaviour>())
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
            if (withSmoothing)
            {
                UpdatePoseWithSmoothing(movement);
            }
            else
            {
                UpdatePose(movement);
            }
        }

        private void UpdatePoseWithSmoothing(AvatarMovementData movement)
        {
            _previousReceivedData = _lastReceivedData;
            _lastReceivedData = movement;

            if (_previousReceivedData == null) return;

            var t = Mathf.Clamp01((Time.realtimeSinceStartup - _lastReceiveTime) / (1.0f / linearInterpolationRate));
            _lastReceiveTime = Time.realtimeSinceStartup;

            foreach (var bone in _mesh.bones)
            {
                if (_lastReceivedData.Transforms.TryGetValue(bone.name, out var lastFoundBone) && _previousReceivedData.Transforms.TryGetValue(bone.name, out var prevFoundBone))
                {
                    bone.SetPositionAndRotation(
                        Vector3.Lerp(
                            new Vector3(prevFoundBone.Pos.X, prevFoundBone.Pos.Y, prevFoundBone.Pos.Z),
                            new Vector3(lastFoundBone.Pos.X, lastFoundBone.Pos.Y, lastFoundBone.Pos.Z),
                            t
                        ),
                        Quaternion.Slerp(
                            new Quaternion(prevFoundBone.Rot.X, prevFoundBone.Rot.Y, prevFoundBone.Rot.Z, prevFoundBone.Rot.W),
                            new Quaternion(lastFoundBone.Rot.X, lastFoundBone.Rot.Y, lastFoundBone.Rot.Z, lastFoundBone.Rot.W),
                            t
                        )
                    );
                }
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
