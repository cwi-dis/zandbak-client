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

        public event Action<User> OnUserJoined;
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
