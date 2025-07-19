using Orchestrator.Data;
using UnityEngine;
using User = Orchestrator.App.User;

namespace Orchestrator.Behaviour
{
    public class RemoteAvatar : MonoBehaviour
    {
        private User _user;
        private SkinnedMeshRenderer _mesh;

        /// <summary>
        /// Initializes the RemoteAvatar instance with a specified user.
        /// </summary>
        /// <param name="user">The user object that represents the avatar's associated user.</param>
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
            _user.OnAvatarMovementReceived += MovementReceived;
        }

        private void MovementReceived(AvatarMovementData movement)
        {
            foreach (var bone in _mesh.bones) {
                if (movement.bones.TryGetValue(bone.name, out var foundBone)) {
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
