using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orchestrator.Data;
using Orchestrator.Wrapping;

namespace Orchestrator.App
{
    public class Session
    {
        private readonly Orchestrator _orchestrator;
        private Data.Session _sessionData;

        public Data.Session SessionData {
            set => _sessionData = value;
        }

        public string Id => _sessionData.Id;
        public string Name => _sessionData.Name;
        public string Status => _sessionData.Status;
        public string Description => _sessionData.Description;

        public List<Presentation> Presentations => _sessionData.Presentations.ToList();
        public Presentation CurrentPresentation;

        public List<ChatMessage> Chat { get; private set; }

        public List<User> RaisedHands { get; private set; }
        public List<User> Users { get; private set; }
        public User Self => _orchestrator.Self;
        public List<User> Speakers => Users.Where(u => u.IsSpeaking).ToList();

        /// <summary>
        /// Occurs when a user joins the current session.
        /// </summary>
        /// <remarks>
        /// This event is triggered whenever a new user is added to the session. The event provides
        /// the user who joined as an argument, allowing subscriber methods to access the user's data.
        /// </remarks>
        public event Action<User> OnUserJoined;

        /// <summary>
        /// Occurs when a user leaves the current session.
        /// </summary>
        /// <remarks>
        /// This event is triggered whenever a user is removed from the session. The event provides
        /// the user who left as an argument, allowing subscriber methods to access the user's data.
        /// </remarks>
        public event Action<User> OnUserLeft;

        /// <summary>
        /// Occurs when the current presentation in the session is updated or changed.
        /// </summary>
        /// <remarks>
        /// This event is triggered whenever the active presentation for a session is modified.
        /// Subscriber methods receive the updated presentation object as an argument,
        /// allowing them to respond to changes in the active presentation.
        /// </remarks>
        public event Action<Presentation> OnPresentationChanged;

        /// <summary>
        /// Occurs when the current presentation in the session changes it's <c>isSharing</c> flag.
        /// </summary>
        /// <remarks>
        /// This event is triggered whenever the active presentation for a session is modified.
        /// Subscriber methods receive the updated presentation object as an argument,
        /// allowing them to respond to changes in the active presentation.
        /// </remarks>
        public event Action<Presentation> OnPresentationIsSharingChanged;

        /// <summary>
        /// Occurs when the current presentation's slide in the session is updated or changed.
        /// </summary>
        /// <remarks>
        /// This event is triggered whenever the slide of the current presentation for a session is modified.
        /// Subscriber methods receive the updated presentation object as an argument,
        /// allowing them to respond to changes in the active presentation.
        /// </remarks>
        public event Action<Presentation> OnPresentationSlideChanged;

        /// <summary>
        /// Occurs when the status of the session changes.
        /// </summary>
        /// <remarks>
        /// This event is triggered whenever the session's status is updated. The event provides the new
        /// session status as a string argument, enabling subscriber methods to respond to status changes.
        /// </remarks>
        public event Action<string> OnSessionStatusChanged;

        /// <summary>
        /// Occurs when a user raised their hand
        /// </summary>
        /// <remarks>
        /// This event is triggered whenever a user in the session raises their hand. The event provides the user that
        /// raised their hand as an argument.
        /// </remarks>
        public event Action<User> OnUserRaisedHand;

        /// <summary>
        /// Occurs when a raised hand has been cleared.
        /// </summary>
        /// <remarks>
        /// This event is triggered whenever a user's raised hand in the session is cleared. The event provides the
        /// user that raised their hand as an argument.
        /// </remarks>
        public event Action<User> OnUserClearedRaisedHand;

        /// <summary>
        /// Triggered when a new message is received in the session.
        /// </summary>
        /// <remarks>
        /// This event is invoked whenever a user in the session sends a message. The event provides the message
        /// content as a parameter, allowing subscribed methods to access the message details for processing or display.
        /// </remarks>
        public event Action<ChatMessage> OnMessageReceived;

        /// <summary>
        /// Triggered when a user changes their <c>isSpeaking</c> status
        /// </summary>
        /// <remarks>
        /// This event is invoked whenever a user changes their isSpeaking flag. The event provides the user and the
        /// new value of the flag.
        /// </remarks>
        public event Action<User, bool> OnIsSpeakingChanged;

        public Session(Orchestrator orchestrator, Data.Session sessionData)
        {
            _sessionData = sessionData;
            _orchestrator = orchestrator;

            Users = _sessionData.UserDefinitions.Select(u => new User(u)).ToList();
            RaisedHands = _sessionData.RaisedHands.Select(u => new User(u)).ToList();
            Chat = _sessionData.Chat.ToList();

            OrchestratorController.Instance.OnUserJoinSessionEvent += UserJoined;
            OrchestratorController.Instance.OnUserLeaveSessionEvent += UserLeft;

            OrchestratorController.Instance.OnSessionPresentationChangedEvent += PresentationChanged;
            OrchestratorController.Instance.OnSessionPresentationSlideChangedEvent += PresentationSlideChanged;
            OrchestratorController.Instance.OnSessionPresentationIsSharingEvent += PresentationIsSharingChanged;

            OrchestratorController.Instance.OnSessionStatusChangedEvent += SessionStatusChanged;

            OrchestratorController.Instance.OnUserRaisedHandEvent += UserRaisedHand;
            OrchestratorController.Instance.OnUserClearedRaisedHandEvent += UserClearedRaisedHand;

            OrchestratorController.Instance.OnUserMessageReceivedEvent += UserMessageReceived;

            OrchestratorController.Instance.OnSessionIsSpeakingEvent += IsSpeakingChanged;
        }

        /// <summary>
        /// Retrieves the latest session information from the orchestrator. This includes all session information and
        /// data such as users currently in the session.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result is the updated session object.</returns>
        public Task<Session> Info()
        {
            var tcs = new TaskCompletionSource<Session>();

            Action<Data.Session> fn = null;
            fn = (sessionData) =>
            {
                _sessionData = sessionData;
                tcs.SetResult(this);
                OrchestratorController.Instance.OnSessionInfoEvent -= fn;
            };

            OrchestratorController.Instance.OnSessionInfoEvent += fn;
            OrchestratorController.Instance.GetSessionInfo();

            return tcs.Task;
        }

        /// <summary>
        /// Leaves this session.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result is a boolean indicating whether the session was left successfully.</returns>
        public Task<bool> Leave()
        {
            var tcs = new TaskCompletionSource<bool>();

            OrchestratorController.Instance.LeaveSession();
            Self.Session = null;

            tcs.SetResult(true);
            return tcs.Task;
        }

        /// <summary>
        /// Advances the current session to the next presentation, if available.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result is the next presentation object.</returns>
        public Task<Presentation> GoToNextPresentation()
        {
            var tcs = new TaskCompletionSource<Presentation>();

            Action<Presentation> fn = null;
            fn = (presentation) =>
            {
                tcs.SetResult(presentation);
                CurrentPresentation = presentation;
                OrchestratorController.Instance.OnSessionPresentationChangedEvent -= fn;
            };

            OrchestratorController.Instance.OnSessionPresentationChangedEvent += fn;
            OrchestratorController.Instance.GoToNextPresentation();

            return tcs.Task;
        }

        /// <summary>
        /// Changes the current slide in the presentation by a specified offset.
        /// </summary>
        /// <param name="slideOffset">The number of slides to move forward or backward. Positive values move forward, while negative values move backward.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is the updated presentation object after the slide has been changed.</returns>
        public Task<Presentation> ChangePresentationSlide(int slideOffset)
        {
            var tcs = new TaskCompletionSource<Presentation>();

            Action<Presentation> fn = null;
            fn = (presentation) =>
            {
                tcs.SetResult(presentation);
                CurrentPresentation.CurrentSlide = presentation.CurrentSlide;
                OrchestratorController.Instance.OnSessionPresentationSlideChangedEvent -= fn;
            };

            OrchestratorController.Instance.OnSessionPresentationSlideChangedEvent += fn;
            OrchestratorController.Instance.ChangeSlide(slideOffset);

            return tcs.Task;
        }

        /// <summary>
        /// Toggles the sharing state of the current presentation in the session.
        /// </summary>
        /// <param name="isSharing">A boolean indicating whether to start (true) or stop (false) sharing the current presentation.</param>
        /// <returns>A task representing the asynchronous operation. The task result is the updated presentation object that reflects the new sharing state.</returns>
        public Task<Presentation> SharePresentation(bool isSharing)
        {
            var tcs = new TaskCompletionSource<Presentation>();

            Action<Presentation> fn = null;
            fn = (presentation) =>
            {
                tcs.SetResult(presentation);
                CurrentPresentation.IsSharing = presentation.IsSharing;
                OrchestratorController.Instance.OnSessionPresentationIsSharingEvent -= fn;
            };

            OrchestratorController.Instance.OnSessionPresentationIsSharingEvent += fn;
            OrchestratorController.Instance.SetCurrentPresentationIsSharing(isSharing);

            return tcs.Task;
        }

        /// <summary>
        /// Updates the current session status in the Orchestrator.
        /// </summary>
        /// <param name="sessionStatus">The new status to set for the current session.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated session status as a string.</returns>
        public Task<string> SetSessionStatus(string sessionStatus)
        {
            var tcs = new TaskCompletionSource<string>();

            Action<string> fn = null;
            fn = (status) =>
            {
                tcs.SetResult(status);
                OrchestratorController.Instance.OnSessionStatusChangedEvent -= fn;
            };

            OrchestratorController.Instance.OnSessionStatusChangedEvent += fn;
            OrchestratorController.Instance.ChangeSessionStatus(sessionStatus);

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
            fn = () =>
            {
                tcs.SetResult(true);
                GetRaisedHands();
                OrchestratorController.Instance.OnRaisedHandEvent -= fn;
            };

            OrchestratorController.Instance.OnRaisedHandEvent += fn;
            OrchestratorController.Instance.RaiseHand();

            return tcs.Task;
        }

        /// <summary>
        /// Retrieves a list of users who have currently raised their hands in the session.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of users with raised hands.</returns>
        public Task<List<User>> GetRaisedHands()
        {
            var tcs = new TaskCompletionSource<List<User>>();

            Action<List<Data.User>> fn = null;
            fn = (users) =>
            {
                RaisedHands = users.Select((u) => new User(u)).ToList();
                tcs.SetResult(RaisedHands);

                OrchestratorController.Instance.OnGetRaisedHandsEvent -= fn;
            };

            OrchestratorController.Instance.OnGetRaisedHandsEvent += fn;
            OrchestratorController.Instance.GetRaisedHands();

            return tcs.Task;
        }

        /// <summary>
        /// Clears the raised hand status of a user in the session. This will trigger an event
        /// when the operation is completed.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose raised hand status is to be cleared.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the operation was successful.</returns>
        public Task<bool> ClearRaisedHand(string userId)
        {
            var tcs = new TaskCompletionSource<bool>();

            Action fn = null;
            fn = () =>
            {
                tcs.SetResult(true);
                GetRaisedHands();
                OrchestratorController.Instance.OnClearRaisedHandEvent -= fn;
            };

            OrchestratorController.Instance.OnClearRaisedHandEvent += fn;
            OrchestratorController.Instance.ClearRaisedHand(userId);

            return tcs.Task;
        }

        /// <summary>
        /// Sends a given chat message to all users in the current session.
        /// </summary>
        /// <param name="message">Message to be sent</param>
        public Task<bool> SendMessage(string message)
        {
            var tcs = new TaskCompletionSource<bool>();

            OrchestratorController.Instance.SendMessageToAll(message);
            tcs.SetResult(true);

            return tcs.Task;
        }

        /// <summary>
        /// Sends a given chat message to a given recipient. The recipient must be in the same session as the current
        /// user.
        /// </summary>
        /// <param name="recipient">Recipient of the message</param>
        /// <param name="message">Message to be sent</param>
        public Task<bool> SendMessage(User recipient, string message)
        {
            var tcs = new TaskCompletionSource<bool>();

            OrchestratorController.Instance.SendMessage(message, recipient.Id);
            tcs.SetResult(true);

            return tcs.Task;
        }

        /// <summary>
        /// Retrieves the chat messages for the current session.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of chat messages.</returns>
        public Task<List<ChatMessage>> GetChatMessages()
        {
            var tcs = new TaskCompletionSource<List<ChatMessage>>();

            Action<List<ChatMessage>> fn = null;
            fn = (messages) =>
            {
                Chat = messages;
                tcs.SetResult(Chat);

                OrchestratorController.Instance.OnGetMessagesEvent -= fn;
            };

            OrchestratorController.Instance.OnGetMessagesEvent += fn;
            OrchestratorController.Instance.GetMessages();

            return tcs.Task;
        }

        #region events

        private void UserJoined(string userId, Data.User userData)
        {
            var joinedUser = new User(userData);

            if (Users.Find(u => u.Id == userId) == null)
            {
                Users.Add(joinedUser);
            }

            OnUserJoined?.Invoke(joinedUser);
        }

        private void UserLeft(string userId)
        {
            var userToRemove = Users.Find(user => user.Id == Id);

            if (userToRemove != null)
            {
                Users.Remove(userToRemove);
                OnUserLeft?.Invoke(userToRemove);
            }
        }

        private void PresentationChanged(Presentation presentation)
        {
            CurrentPresentation = presentation;
            OnPresentationChanged?.Invoke(presentation);
        }

        private void PresentationSlideChanged(Presentation presentation)
        {
            CurrentPresentation.CurrentSlide = presentation.CurrentSlide;
            OnPresentationSlideChanged?.Invoke(presentation);
        }

        private void PresentationIsSharingChanged(Presentation presentation)
        {
            CurrentPresentation.IsSharing = presentation.IsSharing;
            OnPresentationIsSharingChanged?.Invoke(presentation);
        }

        private void SessionStatusChanged(string status)
        {
            _sessionData.Status = status;
            OnSessionStatusChanged?.Invoke(status);
        }

        private async void UserRaisedHand(string userId)
        {
            var users = await GetRaisedHands();
            var raisedHandUser = users.Find(u => u.Id == userId);
            OnUserRaisedHand?.Invoke(raisedHandUser);
        }

        private void UserClearedRaisedHand(string userId)
        {
            GetRaisedHands();

            var clearedRaisedHandUser = Users.Find(u => u.Id == userId);
            OnUserClearedRaisedHand?.Invoke(clearedRaisedHandUser);
        }

        private void UserMessageReceived(ChatMessage message)
        {
            OnMessageReceived?.Invoke(message);
        }

        private void IsSpeakingChanged(Data.User user, bool isSpeaking)
        {
            var foundUser = Users.Find(u => u.Id == user.Id);
            if (foundUser == null) return;

            OnIsSpeakingChanged?.Invoke(foundUser, isSpeaking);
        }

        #endregion
    }
}
