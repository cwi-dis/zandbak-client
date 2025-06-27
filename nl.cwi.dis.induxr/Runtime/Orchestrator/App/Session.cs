using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orchestrator.Wrapping;

namespace Orchestrator.App
{
    public class Session
    {
        private readonly Orchestrator _orchestrator;
        private Data.Session _sessionData;

        public string Id => _sessionData.Id;
        public string Name => _sessionData.Name;
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

        public Session(Orchestrator orchestrator, Data.Session sessionData)
        {
            _sessionData = sessionData;
            _orchestrator = orchestrator;

            OrchestratorController.Instance.OnUserJoinSessionEvent += UserJoined;
            OrchestratorController.Instance.OnUserLeaveSessionEvent += UserLeft;
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
    }
}
