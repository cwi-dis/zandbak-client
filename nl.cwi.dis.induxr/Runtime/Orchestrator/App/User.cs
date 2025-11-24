#pragma warning disable CS0618

using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Orchestrator.Data;
using Orchestrator.Wrapping;
using UnityEngine;

namespace Orchestrator.App
{
    public class User
    {
        private readonly Orchestrator _orchestrator;

        public Data.User UserData { set; get; }

        public Session Session => _orchestrator.CurrentSession;
        public Bubble Bubble => _orchestrator?.CurrentSession?.CurrentBubble;

        public bool IsInSession => Session != null;
        public bool IsInBubble => Bubble != null;

        public string Id => UserData.Id;
        public string Name => UserData.Username;
        /// <summary>
        /// Indicates whether the user is currently speaking
        /// </summary>
        public bool IsSpeaking => UserData.IsSpeaking;
        /// <summary>
        /// Returns the type of user (e.g. presenter, moderator, user)
        /// </summary>
        public string Type => UserData.UserType;
        /// <summary>
        /// Indicates whether the user has their hand raised currently
        /// </summary>
        public bool HasHandRaised => UserData.HasHandRaised;
        /// <summary>
        /// Returns the type of device that the user has used to connect
        /// </summary>
        public string DeviceType => UserData.DeviceType;

        /// <summary>
        /// Returns the user's current status (e.g. 'available', 'in a meeting', ...)
        /// </summary>
        public string Status => UserData.Status;

        public AvatarMovementData Transform => (UserData.Transform == null) ? null : new AvatarMovementData()
        {
            UserId = UserData.Id,
            Timestamp = UserData.Transform.Timestamp,
            Bones = UserData.Transform.Bones.Select((pair) => new { pair.Key, Value = new BoneData()
            {
                Pos = new PositionData() { X = pair.Value.Pos.X, Y = pair.Value.Pos.Y, Z = pair.Value.Pos.Z },
                Rot = new RotationData() { X = pair.Value.Rot.X, Y = pair.Value.Rot.Y, Z = pair.Value.Rot.Z, W = pair.Value.Rot.W }
            }}).ToDictionary(x => x.Key, x => x.Value)
        };

        /// <summary>
        /// Event triggered when avatar movement data is received for this user.
        /// </summary>
        public event Action<AvatarMovementData> OnAvatarMovementReceived;

        /// <summary>
        /// Event triggered when the user raises or clears their raised hand. The boolean parameter indicates whether
        /// the hand was raised (true) or cleared (false).
        /// </summary>
        public event Action<bool> OnHandRaised;

        /// <summary>
        /// Event triggered when the isSpeaking flag for the user changes. The boolean parameter indicates whether
        /// the user is speaking (true) or not (false).
        /// </summary>
        public event Action<bool> OnIsSpeaking;

        public User(Orchestrator orchestrator, Data.User userData)
        {
            UserData = userData;
            _orchestrator = orchestrator;
        }

        /// <summary>
        /// Configures the user to respond to events in the current session. If the session is null, the method logs a
        /// warning and does not attach any event listeners. This method should be called after joining a new session.
        /// </summary>
        public void Join()
        {
            var session = _orchestrator.CurrentSession;

            if (session != null)
            {
                session.OnUserRaisedHand += HandRaised;
                session.OnUserClearedRaisedHand += HandRaised;
                session.OnIsSpeakingChanged += IsSpeakingChanged;

                if (Id != _orchestrator.Self.Id)
                {
                    Debug.Log($"Enabling broadcasts for {Name}");
                    EnableMovementBroadcastListener();
                }
            }
            else
            {
                Debug.LogWarning("Session is null. Cannot setup hand raised listeners for user " + Name);
            }
        }

        public void Leave()
        {
            var session = _orchestrator.CurrentSession;

            if (session != null)
            {
                session.OnUserRaisedHand -= HandRaised;
                session.OnUserClearedRaisedHand -= HandRaised;
                session.OnIsSpeakingChanged -= IsSpeakingChanged;
            }

            DisableMovementBroadcastListener();
        }

        /// <summary>
        /// Sends a given chat message to this user as a recipient. The recipient must be in the same session as the current
        /// user.
        /// </summary>
        /// <param name="message">Message to be sent</param>
        public Task<bool> SendMessage(string message)
        {
            var tcs = new TaskCompletionSource<bool>();

            OrchestratorController.Instance.SendMessage(message, Id);
            tcs.SetResult(true);

            return tcs.Task;
        }

        /// <summary>
        /// Enables the reception of avatar movement broadcasts for the user in the current session. If the user is not
        /// in any session, broadcasts will not be enabled and a warning is logged.
        /// </summary>
        private void EnableMovementBroadcastListener()
        {
            var session = _orchestrator.CurrentSession;

            if (session != null)
            {
                session.OnBroadcastDataReceived += BroadcastReceived;
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
        private void DisableMovementBroadcastListener()
        {
            if (_orchestrator.CurrentSession == null)
                return;

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

            if (movement.UserId != Id) return;
            OnAvatarMovementReceived?.Invoke(movement);
        }

        private void HandRaised(User user)
        {
            if (user.Id != Id)
                return;

            OnHandRaised?.Invoke(user.HasHandRaised);
        }

        private void IsSpeakingChanged(User user, bool isSpeaking)
        {
            if (user.Id != Id)
                return;

            OnIsSpeaking?.Invoke(isSpeaking);
            UserData.IsSpeaking = isSpeaking;
        }
    }

    public class SelfUser : User
    {
        public GameObject Avatar;

        public SelfUser(Orchestrator orchestrator, Data.User userData) : base(orchestrator, userData) {}

        /// <summary>
        /// Updates the status of the current user asynchronously. This method listens for a status change
        /// event and updates the user data upon receiving the event.
        /// </summary>
        /// <param name="status">The new status to set for the user.</param>
        /// <returns>A task that resolves with the updated user when the status change is confirmed.</returns>
        public Task<User> SetStatus(string status)
        {
            var tcs = new TaskCompletionSource<User>();

            Action<Data.User, string> fn = null;
            fn = (u, newStatus) =>
            {
                if (u.Id != Id) return;

                UserData.Status = newStatus;
                tcs.SetResult(this);
                OrchestratorController.Instance.OnUserStatusChangedEvent -= fn;
            };

            OrchestratorController.Instance.OnUserStatusChangedEvent += fn;
            OrchestratorController.Instance.ChangeUserStatus(status);

            return tcs.Task;
        }

        /// <summary>
        /// Updates the speaking status for the current user
        /// </summary>
        /// <param name="isSpeaking">The new speaking status to set for the current user.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task<bool> SetIsSpeaking(bool isSpeaking)
        {
            var tcs = new TaskCompletionSource<bool>();

            Action<bool> fn = null;
            fn = (result) =>
            {
                tcs.SetResult(result);
                OrchestratorController.Instance.OnIsSpeakingEvent -= fn;
            };

            OrchestratorController.Instance.OnIsSpeakingEvent += fn;
            OrchestratorController.Instance.IsSpeaking(isSpeaking);

            return tcs.Task;
        }

        /// <summary>
        /// Initiates a "raise hand" action for the current user and waits for the Orchestrator's acknowledgement.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the user ID of the user who raised their hand.</returns>
        public Task<bool> RaiseHand()
        {
            var tcs = new TaskCompletionSource<bool>();

            Action fn = null;
            fn = async () =>
            {
                await Session.GetRaisedHands();
                tcs.SetResult(true);
                OrchestratorController.Instance.OnRaisedHandEvent -= fn;
            };

            OrchestratorController.Instance.OnRaisedHandEvent += fn;
            OrchestratorController.Instance.RaiseHand();

            return tcs.Task;
        }


        /// <summary>
        /// Clears the raised hand status of the current user in the session. This will trigger an event
        /// when the operation is completed.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the operation was successful.</returns>
        public Task<bool> ClearRaisedHand()
        {
            var tcs = new TaskCompletionSource<bool>();

            Action fn = null;
            fn = () =>
            {
                tcs.SetResult(true);
                Session.GetRaisedHands();
                OrchestratorController.Instance.OnClearRaisedHandEvent -= fn;
            };

            OrchestratorController.Instance.OnClearRaisedHandEvent += fn;
            OrchestratorController.Instance.ClearRaisedHand();

            return tcs.Task;
        }
    }
}

#pragma warning disable CS0618
