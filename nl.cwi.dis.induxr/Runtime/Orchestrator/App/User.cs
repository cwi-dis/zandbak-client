using System;
using Orchestrator.Behaviour;
using Orchestrator.Data;
using UnityEngine;

namespace Orchestrator.App
{
    public class User
    {
        private readonly Data.User _userData;
        private readonly Orchestrator _orchestrator;

        public Session Session { get; set; }
        public string Id => _userData.Id;
        public string Name => _userData.Username;
        /// <summary>
        /// Indicates whether the user is currently speaking
        /// </summary>
        public bool IsSpeaking => _userData.IsSpeaking;
        /// <summary>
        /// Returns the type of user (e.g. presenter, moderator, user)
        /// </summary>
        public string Type => _userData.UserType;
        /// <summary>
        /// Indicates whether the user has their hand raised currently
        /// </summary>
        public bool HasHandRaised => _userData.HasHandRaised;
        /// <summary>
        /// Returns the type of device that the user has used to connect
        /// </summary>
        public string DeviceType => _userData.DeviceType;

        /// <summary>
        /// Event triggered when avatar movement data is received for this user.
        /// </summary>
        public event Action<AvatarMovementData> OnAvatarMovementReceived;

        public User(Orchestrator orchestrator, Data.User userData)
        {
            _userData = userData;
            _orchestrator = orchestrator;

            var session = _orchestrator.CurrentSession;
            if (session != null)
            {
                session.OnBroadcastDataReceived += BroadcastReceived;
            }
        }

        /// <summary>
        /// Broadcasts avatar movement data to all users in the current session.
        /// </summary>
        /// <param name="data">The movement data of the avatar, including user ID, bone data, and timestamp.</param>
        public void BroadcastAvatarMovement(AvatarMovementData data)
        {
            _orchestrator.CurrentSession?.BroadcastTransform(data);
        }

        private void BroadcastReceived(BroadcastData data)
        {
            if (data.Channel != "transform") return;
            var movement = JsonUtility.FromJson<AvatarMovementData>(data.Data);

            if (movement.userId != Id) return;
            OnAvatarMovementReceived?.Invoke(movement);
        }
    }
}
