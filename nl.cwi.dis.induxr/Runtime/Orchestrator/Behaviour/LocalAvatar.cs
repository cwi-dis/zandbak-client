using System.Collections.Generic;
using Orchestrator.Data;
using UnityEngine;
using User = Orchestrator.App.User;

namespace Orchestrator.Behaviour
{
    public class LocalAvatar : MonoBehaviour
    {
        public int updateRate = 10;

        private User _user;
        private SkinnedMeshRenderer _mesh;
        private float _timer;

        /// <summary>
        /// Initializes the local avatar with the specified user.
        /// </summary>
        /// <param name="user">The User object to associate with the local avatar.</param>
        public void Initialize(User user)
        {
            _user = user;
        }

        private void Start()
        {
            if (_user == null)
            {
                Debug.LogError("User is null. Make sure to call Initialize()");
                return;
            }

            _mesh = GetComponentInChildren<SkinnedMeshRenderer>();
        }

        private void Update()
        {
            _timer += Time.deltaTime;

            if (_timer >= 1f / updateRate)
            {
                _timer -= 1f / updateRate;

                var data = GetBoneData();
                _user.BroadcastAvatarMovement(data);
            }
        }

        private AvatarMovementData GetBoneData()
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
                UserId = _user.Id,
                Timestamp = Time.time,
                Bones = boneData
            };
        }
    }
}
