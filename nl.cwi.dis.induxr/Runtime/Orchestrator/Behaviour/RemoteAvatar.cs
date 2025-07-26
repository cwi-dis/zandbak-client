using Orchestrator.Data;
using UnityEngine;
using User = Orchestrator.App.User;

namespace Orchestrator.Behaviour
{
    public class RemoteAvatar : MonoBehaviour
    {
        public bool withSmoothing = false;
        public int linearInterpolationRate = 10;
        public GameObject notification;

        private User _user;
        private SkinnedMeshRenderer _mesh;

        private AvatarMovementData _previousReceivedData;
        private AvatarMovementData _lastReceivedData;
        private float _lastReceiveTime;

        /// <summary>
        /// Initializes the RemoteAvatar instance with a specified user.
        /// </summary>
        /// <param name="user">The user object that represents the avatar's associated user.</param>
        public void Initialize(User user)
        {
            _user = user;
        }

        private void Awake()
        {
            _lastReceiveTime = Time.realtimeSinceStartup;
        }

        private void Start()
        {
            if (_user == null)
            {
                Debug.LogError("User is null. Make sure to call Initialize()");
                return;
            }

            _mesh = GetComponentInChildren<SkinnedMeshRenderer>();

            if (_user.Transform != null)
            {
                UpdateBones(_user.Transform);
            }

            _user.OnAvatarMovementReceived += MovementReceived;
            _user.OnHandRaised += (isRaised) => notification.SetActive(isRaised);
            _user.OnIsSpeaking += (isSpeaking) => Debug.Log($"{_user.Name} is speaking: {isSpeaking}");
        }

        private void MovementReceived(AvatarMovementData movement)
        {
            if (withSmoothing)
            {
                UpdateBonesWithSmoothing(movement);
            }
            else
            {
                UpdateBones(movement);
            }
        }

        private void UpdateBonesWithSmoothing(AvatarMovementData movement)
        {
            _previousReceivedData = _lastReceivedData;
            _lastReceivedData = movement;

            if (_previousReceivedData == null) return;

            var t = Mathf.Clamp01((Time.realtimeSinceStartup - _lastReceiveTime) / (1.0f / linearInterpolationRate));
            _lastReceiveTime = Time.realtimeSinceStartup;

            foreach (var bone in _mesh.bones)
            {
                if (_lastReceivedData.Bones.TryGetValue(bone.name, out var lastFoundBone) && _previousReceivedData.Bones.TryGetValue(bone.name, out var prevFoundBone))
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

        private void UpdateBones(AvatarMovementData movement)
        {
            foreach (var bone in _mesh.bones)
            {
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
