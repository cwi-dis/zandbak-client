using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orchestrator.Data;
using Orchestrator.Wrapping;

namespace Orchestrator.App
{
    public class Session
    {
        private readonly Orchestrator _orchestrator;
        private Data.Session _sessionData;

        public string Id => _sessionData.Id;
        public string Name => _sessionData.Name;
        public string Status => _sessionData.Status;

        public List<Presentation> Presentations { get; } = new();
        public Presentation CurrentPresentation;

        public List<User> Users { get; } = new();
        public User Self => _orchestrator.Self;

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

        public Session(Orchestrator orchestrator, Data.Session sessionData)
        {
            _sessionData = sessionData;
            _orchestrator = orchestrator;

            OrchestratorController.Instance.OnUserJoinSessionEvent += UserJoined;
            OrchestratorController.Instance.OnUserLeaveSessionEvent += UserLeft;
            OrchestratorController.Instance.OnSessionPresentationChangedEvent += PresentationChanged;
            OrchestratorController.Instance.OnSessionPresentationSlideChangedEvent += PresentationSlideChanged;
            OrchestratorController.Instance.OnSessionStatusChangedEvent += SessionStatusChanged;
        }

        public void Update(Data.Session sessionData)
        {
            _sessionData = sessionData;
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
                OrchestratorController.Instance.OnSessionPresentationSlideChangedEvent -= fn;
            };

            OrchestratorController.Instance.OnSessionPresentationSlideChangedEvent += fn;
            OrchestratorController.Instance.ChangeSlide(slideOffset);

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

        private void UserJoined(string userId, Data.User userData)
        {
            var joinedUser = new User(userData);
            Users.Add(joinedUser);

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

        private void SessionStatusChanged(string status)
        {
            _sessionData.Status = status;
            OnSessionStatusChanged?.Invoke(status);
        }
    }
}
