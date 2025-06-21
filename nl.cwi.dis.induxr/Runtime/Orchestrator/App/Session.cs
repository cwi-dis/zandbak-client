using System;
using System.Linq;
using Orchestrator.Wrapping;
using UnityEngine.PlayerLoop;

namespace Orchestrator.App
{
    public class Session
    {
        private Data.Session _sessionData;

        public string Id => _sessionData.sessionId;

        public Session(Data.Session sessionData)
        {
            _sessionData = sessionData;
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
    }
}
