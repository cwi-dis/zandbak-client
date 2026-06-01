using System;
using System.Linq;
using Orchestrator.Data;
using UnityEngine;
using SelfUser = Orchestrator.App.SelfUser;

namespace Orchestrator.Behaviour.Avatar
{
    [Obsolete]
    public class LocalAvatar : MonoBehaviour
    {
        [Tooltip("How many times a second the pose data should be broadcast to the server.")]
        public int updateRate = 10;

        [Header("Notification Icon")]
        [Tooltip("A reference to the object to be used as notification icon. If given, the notification icon will be shown when the user's hand is raised.")]
        public GameObject notification;

        private SelfUser _user;
        private SkinnedMeshRenderer _mesh;
        private float _timer;

        /// <summary>
        /// Initializes the local avatar with the specified user.
        /// </summary>
        /// <param name="user">The user to associate with the local avatar.</param>
        public void Initialize(SelfUser user)
        {
            _user = user;
            _user.Avatar = gameObject;
        }

        private void Start()
        {
            // Print an error if no user is assigned (i.e. Initialize() wasn't called)
            if (_user == null)
            {
                Debug.LogError("User is null. Make sure to call Initialize()");
                return;
            }

            // Get SkinnedMeshRenderer
            _mesh = GetComponentInChildren<SkinnedMeshRenderer>();

            // Add a handler for hand raising event if a reference to the notification object is given
            if (notification != null)
            {
                _user.OnHandRaised += (isRaised) => notification.SetActive(isRaised);
            }
        }

        private void Update()
        {
            _timer += Time.deltaTime;

            // Only send the bone transformation update if the interval given by updateRate has elapsed
            if (_timer >= 1f / updateRate)
            {
                _timer -= 1f / updateRate;

                // Get bone data
                var data = GetBoneData();
                // Broadcast bone transforms to the current session
                _user.BroadcastAvatarMovement(data);
            }
        }

        private AvatarMovementData GetBoneData()
        {
            // Get the bones from the SkinnedMeshRenderer and create a dictionary with the bone name as a key and the
            // position and rotation of the bone as value
            var boneData = _mesh.bones.ToDictionary(bone => bone.name, bone => new BoneData
            {
                Pos = new PositionData { X = bone.position.x, Y = bone.position.y, Z = bone.position.z },
                Rot = new RotationData { X = bone.rotation.x, Y = bone.rotation.y, Z = bone.rotation.z, W = bone.rotation.w },
            });

            // Return AvatarMovementData object with user id, timestamp and bone data
            return new AvatarMovementData {
                UserId = _user.Id,
                Timestamp = Time.time,
                Transforms = boneData
            };
        }
    }
}
