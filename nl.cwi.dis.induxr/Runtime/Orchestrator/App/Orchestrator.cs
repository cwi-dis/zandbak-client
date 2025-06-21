using System;
using System.Collections.Generic;
using System.Linq;
using Orchestrator.Wrapping;
using UnityEngine;

namespace Orchestrator.App
{
    public class Orchestrator : MonoBehaviour
    {
        public List<Session> Sessions { get; private set; }
        public Session CurrentSession { get; private set; }

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

        public void GetSessions(Action<List<Session>> callback)
        {
            Action<Data.Session[]> fn = null;
            fn = (sessions) =>
            {
                Sessions = sessions.Select(session => new Session(session)).ToList();
                callback?.Invoke(Sessions);
                OrchestratorController.Instance.OnSessionsEvent -= fn;
            };

            OrchestratorController.Instance.OnSessionsEvent += fn;
            OrchestratorController.Instance.GetSessions();
        }

        public void JoinSession(string sessionId, Action<Session> callback)
        {
            var sessionToJoin = Sessions.Find((s) => s.Id == sessionId);

            if (sessionToJoin == null)
            {
                Debug.LogError($"Session {sessionId} not found");
                return;
            }

            Action<Data.Session> fn = null;
            fn = (session) =>
            {
                CurrentSession = sessionToJoin;
                callback?.Invoke(CurrentSession);
                OrchestratorController.Instance.OnJoinSessionEvent -= fn;
            };

            OrchestratorController.Instance.OnJoinSessionEvent += fn;
            OrchestratorController.Instance.JoinSession(sessionId);
        }
    }
}