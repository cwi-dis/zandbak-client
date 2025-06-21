using System;
using System.Collections.Generic;
using Orchestrator.Data;
using Orchestrator.Wrapping;
using UnityEngine;

namespace Orchestrator.App
{
    public class Orchestrator : MonoBehaviour
    {
        private List<Session> _sessions;

        public void GetOrchestratorVersion(Action<string> callback)
        {
            Action<string> fn = null;
            fn = (version) =>
            {
                callback?.Invoke(version);
                OrchestratorController.Instance.OnGetOrchestratorVersionEvent -= fn;
            };

            OrchestratorController.Instance.OnGetOrchestratorVersionEvent += fn;
            OrchestratorController.Instance.GetVersion();
        }

        public void GetSessions(Action<Data.Session[]> callback)
        {
            Action<Data.Session[]> fn = null;
            fn = (sessions) =>
            {
                callback?.Invoke(sessions);
                OrchestratorController.Instance.OnSessionsEvent -= fn;
            };

            OrchestratorController.Instance.OnSessionsEvent += fn;
            OrchestratorController.Instance.GetSessions();
        }
    }
}