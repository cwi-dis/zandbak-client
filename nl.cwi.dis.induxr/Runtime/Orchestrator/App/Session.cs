using System;
using System.Collections.Generic;
using Orchestrator.Wrapping;

namespace Orchestrator.App
{
    public class Session
    {
        private Data.Session _sessionData;
        private List<User> _users;

        public string Id => _sessionData.Id;
        public string Name => _sessionData.Name;
        public List<User> Users => _users;

        public Session(Data.Session sessionData)
        {
            _sessionData = sessionData;

            OrchestratorController.Instance.OnUserJoinSessionEvent += UserJoined;
            OrchestratorController.Instance.OnUserLeaveSessionEvent += UserLeft;
        }

        public void Update(Data.Session sessionData)
        {
            _sessionData = sessionData;
        }

        public void Info(Action<Session> callback)
        {
            Action<Data.Session> fn = null;
            fn = (sessionData) =>
            {
                _sessionData = sessionData;
                callback?.Invoke(this);
                OrchestratorController.Instance.OnSessionInfoEvent -= fn;
            };

            OrchestratorController.Instance.OnSessionInfoEvent += fn;
            OrchestratorController.Instance.GetSessionInfo();
        }

        public void Leave(Action callback)
        {
            OrchestratorController.Instance.LeaveSession();
            callback?.Invoke();
        }

        private void UserJoined(string userId, Data.User userData)
        {
            _users.Add(new User(userData));
        }

        private void UserLeft(string userId)
        {
            var userToRemove = _users.Find(user => user.Session.Id == Id);

            if (userToRemove != null)
            {
                _users.Remove(userToRemove);
            }
        }
    }
}
