using Orchestrator.Data;
using UnityEngine;
using User = Orchestrator.App.User;

namespace Orchestrator.Behaviour
{
    public class RemoteAvatar : MonoBehaviour
    {
        [Header("Smoothing options")]
        public bool withSmoothing = false;
        public int linearInterpolationRate = 10;

        [Header("References")]

        [Tooltip("A reference to the object to be used as notification icon. If given, the notification icon will be shown when the user's hand is raised.")]
        public GameObject notification;
        [Tooltip("A reference to a text object to be used for displaying the user's username.")]
        public TextMesh usernamePlaque;

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
            _user.Avatar = gameObject;
        }

        private void Awake()
        {
            _lastReceiveTime = Time.realtimeSinceStartup;
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

            // If the user object has a transform property, update bones immediately
            if (_user.Transform != null)
            {
                UpdateBones(_user.Transform);
            }

            // Set username plaque if a reference for the plaque is assigned
            if (usernamePlaque != null)
            {
                usernamePlaque.text = _user.Name;
            }

            // Add handler for receiving bone transforms
            _user.OnAvatarMovementReceived += MovementReceived;
            // Add a handler for updates to isSpeaking property
            _user.OnIsSpeaking += (isSpeaking) => Debug.Log($"{_user.Name} is speaking: {isSpeaking}");
            // Add a handler for hand raising event if a notification object is assigned
            if (notification != null)
            {
                _user.OnHandRaised += (isRaised) => notification.SetActive(isRaised);
            }
        }

        /// <summary>
        /// This event handler is called every time the associated user object receives a broadcast with bone
        /// transformations. The bone transformation object is passed in as an argument.
        /// </summary>
        /// <param name="movement">The received bone transformation</param>
        private void MovementReceived(AvatarMovementData movement)
        {
            // Call function to update bone transformations with or without smoothing
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
            // Keep track of last received movement data for linear interpolation
            _previousReceivedData = _lastReceivedData;
            _lastReceivedData = movement;

            // Do nothing on the first frame
            if (_previousReceivedData == null) return;

            // Compute the value of t used in linear interpolation
            var t = Mathf.Clamp01((Time.realtimeSinceStartup - _lastReceiveTime) / (1.0f / linearInterpolationRate));
            _lastReceiveTime = Time.realtimeSinceStartup;

            // Iterate through all the bones in the SkinnedMeshRenderer
            foreach (var bone in _mesh.bones)
            {
                // Update bone if the received data and the previously received data contain a value for the given bone
                if (_lastReceivedData.Bones.TryGetValue(bone.name, out var lastFoundBone) && _previousReceivedData.Bones.TryGetValue(bone.name, out var prevFoundBone))
                {
                    // Update the position and rotation of the given bone using linear interpolation
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
            // Iterate through all the bones in the SkinnedMeshRenderer
            foreach (var bone in _mesh.bones)
            {
                // Update bone if the received data contains a value for the given bone
                if (movement.Bones.TryGetValue(bone.name, out var foundBone)) {
                    // Update the position and rotation of the given bone
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
