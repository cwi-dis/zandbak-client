using System;
using Newtonsoft.Json;
using Orchestrator.Data;
using UnityEngine;

namespace Orchestrator.App
{
    public class User
    {
        private readonly Data.User _userData;
        private readonly Orchestrator _orchestrator;

        public Session Session => _orchestrator.CurrentSession;
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
        }

        /// <summary>
        /// Enables the reception of avatar movement broadcasts for the user in the current session. If the user is not
        /// in any session, broadcasts will not be enabled and a warning is logged.
        /// </summary>
        public void EnableMovementBroadcastListener()
        {
            if (_orchestrator.CurrentSession != null)
            {
                _orchestrator.CurrentSession.OnBroadcastDataReceived += BroadcastReceived;
            }
            else
            {
                Debug.LogWarning($"Session for user {Name} is not set. Broadcasts will not be received");
            }
        }

        /// <summary>
        /// Disables the reception of avatar movement broadcasts for the user in the current session.
        /// This stops the session from raising broadcast-related events for the user.
        /// </summary>
        public void DisableMovementBroadcastListener()
        {
            if (_orchestrator.CurrentSession == null) return;
            _orchestrator.CurrentSession.OnBroadcastDataReceived -= BroadcastReceived;
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
            var movement = JsonConvert.DeserializeObject<AvatarMovementData>(data.Data);

            if (movement.userId != Id) return;
            OnAvatarMovementReceived?.Invoke(movement);
        }
    }
}
