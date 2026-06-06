using Orchestrator.App;
using Orchestrator.Data;
using UnityEngine;
using User = Orchestrator.App.User;

namespace Orchestrator.Behaviour.Avatar
{
    public abstract class AvatarBehaviour : MonoBehaviour
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

        protected User User;

        // Local state
        private float _updateTimer;
        private bool _isLocal;

        // Remote state
        protected AvatarPoseData LastReceivedData;
        protected float LastReceivedTime;

        /// <summary>
        /// Initialises the avatar behaviour with the associated user.
        /// Sets up local or remote state based on whether the user is a <see cref="SelfUser"/>.
        /// </summary>
        /// <param name="user">The orchestrator user associated with this avatar.</param>
        public virtual void Initialize(User user)
        {
            User = user;
            User.Avatar = gameObject;
            _isLocal = user is SelfUser;
        }

        /// <summary>
        /// Handles basic initialisation, including setting the username plaque and notification events.
        /// For remote users, it also subscribes to pose updates and disables other behaviours.
        /// </summary>
        protected virtual void Start()
        {
            if (User == null)
            {
                Debug.LogError("User is null. Make sure to call Initialize()");
                return;
            }

            if (usernamePlaque != null)
            {
                usernamePlaque.text = User.Name;
            }

            if (notification != null)
            {
                User.OnHandRaised += (isRaised) => notification.SetActive(isRaised);
            }

            if (_isLocal)
            {
                // hide username plaque for the local user
                Debug.Log("Hiding username plaque for local user");
                usernamePlaque?.gameObject.SetActive(false);
            }
            else
            {
                // Remote specific setup
                LastReceivedTime = Time.realtimeSinceStartup;

                if (User.Transform != null)
                {
                    SetPose(User.Transform);
                }

                User.OnAvatarPoseReceived += OnPoseReceived;
                User.OnIsSpeaking += (isSpeaking) => Debug.Log($"{User.Name} is speaking: {isSpeaking}");

                // Disable all other behaviours for remote avatars
                foreach (var comp in GetComponents<UnityEngine.Behaviour>())
                {
                    if (comp != this) comp.enabled = false;
                }
            }
        }

        /// <summary>
        /// Periodically broadcasts local pose or interpolates remote pose.
        /// </summary>
        protected virtual void Update()
        {
            if (User == null) return;

            if (_isLocal)
            {
                BroadcastPoseWithRate();
            }
            else
            {
                InterpolatePose();
            }
        }

        private void BroadcastPoseWithRate()
        {
            _updateTimer += Time.deltaTime;

            if (_updateTimer >= 1f / updateRate)
            {
                _updateTimer -= 1f / updateRate;

                var data = CollectPoseData();
                ((SelfUser)User).BroadcastAvatarPose(data);
            }
        }

        /// <summary>
        /// Interpolates the avatar's position and rotation for remote users to ensure smooth pose transitions.
        /// This is called every frame on remote avatars.
        /// </summary>
        protected abstract void InterpolatePose();

        /// <summary>
        /// Immediately sets the avatar's position and rotation based on the provided pose data.
        /// Used for initial positioning or hard resets.
        /// </summary>
        /// <param name="pose">The pose data to apply.</param>
        protected abstract void SetPose(AvatarPoseData pose);

        /// <summary>
        /// Collects the current pose data (e.g. bone transforms or root transform) to be broadcast to other users.
        /// </summary>
        /// <returns>A data object containing the current pose of the avatar.</returns>
        protected abstract AvatarPoseData CollectPoseData();

        /// <summary>
        /// Event handler called when new pose data is received from the orchestrator for this user.
        /// </summary>
        /// <param name="pose">The received pose data.</param>
        protected virtual void OnPoseReceived(AvatarPoseData pose)
        {
            LastReceivedData = pose;
            LastReceivedTime = Time.realtimeSinceStartup;
        }
    }
}
