using System;
using Orchestrator.Data;
using UnityEngine;
using User = Orchestrator.App.User;

namespace Orchestrator.Behaviour.Avatar
{
    [Obsolete]
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

        private AvatarPoseData _previousReceivedData;
        private AvatarPoseData _lastReceivedData;
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
            _user.OnAvatarPoseReceived += PoseReceived;
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
        /// <param name="pose">The received bone transformation</param>
        private void PoseReceived(AvatarPoseData pose)
        {
            // Call function to update bone transformations with or without smoothing
            if (withSmoothing)
            {
                UpdateBonesWithSmoothing(pose);
            }
            else
            {
                UpdateBones(pose);
            }
        }

        private void UpdateBonesWithSmoothing(AvatarPoseData pose)
        {
            // Keep track of last received pose data for linear interpolation
            _previousReceivedData = _lastReceivedData;
            _lastReceivedData = pose;

            // Do nothing on the first frame
            if (_previousReceivedData == null) return;

            // Compute the value of t used in linear interpolation
            var t = Mathf.Clamp01((Time.realtimeSinceStartup - _lastReceiveTime) / (1.0f / linearInterpolationRate));
            _lastReceiveTime = Time.realtimeSinceStartup;

            // Iterate through all the bones in the SkinnedMeshRenderer
            foreach (var bone in _mesh.bones)
            {
                // Update bone if the received data and the previously received data contain a value for the given bone
                if (_lastReceivedData.Transforms.TryGetValue(bone.name, out var lastFoundPose) && _previousReceivedData.Transforms.TryGetValue(bone.name, out var prevFoundPose))
                {
                    // Update the position and rotation of the given bone using linear interpolation
                    bone.SetPositionAndRotation(
                        Vector3.Lerp(
                            new Vector3(prevFoundPose.Pos.X, prevFoundPose.Pos.Y, prevFoundPose.Pos.Z),
                            new Vector3(lastFoundPose.Pos.X, lastFoundPose.Pos.Y, lastFoundPose.Pos.Z),
                            t
                        ),
                        Quaternion.Slerp(
                            new Quaternion(prevFoundPose.Rot.X, prevFoundPose.Rot.Y, prevFoundPose.Rot.Z, prevFoundPose.Rot.W),
                            new Quaternion(lastFoundPose.Rot.X, lastFoundPose.Rot.Y, lastFoundPose.Rot.Z, lastFoundPose.Rot.W),
                            t
                        )
                    );
                }
            }
        }

        private void UpdateBones(AvatarPoseData pose)
        {
            // Iterate through all the bones in the SkinnedMeshRenderer
            foreach (var bone in _mesh.bones)
            {
                // Update bone if the received data contains a value for the given bone
                if (pose.Transforms.TryGetValue(bone.name, out var foundPose)) {
                    // Update the position and rotation of the given bone
                    bone.SetPositionAndRotation(new Vector3(
                        foundPose.Pos.X,
                        foundPose.Pos.Y,
                        foundPose.Pos.Z
                    ), new Quaternion(
                        foundPose.Rot.X,
                        foundPose.Rot.Y,
                        foundPose.Rot.Z,
                        foundPose.Rot.W
                    ));
                }
            }
        }
    }
}
