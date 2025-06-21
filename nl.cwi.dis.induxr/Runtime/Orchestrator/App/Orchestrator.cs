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
        public User CurrentUser { get; private set; }

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

        public void Login(string username, string password, Action<bool, string> callback)
        {
            Action<bool, string> fn = null;
            fn = (success, userId) =>
            {
                callback?.Invoke(success, userId);

                if (success)
                {
                    CurrentUser = new User(
                        new Data.User
                        {
                            userName = username,
                            userId = userId
                        }
                    );
                }

                OrchestratorController.Instance.OnLoginEvent -= fn;
            };

            OrchestratorController.Instance.OnLoginEvent += fn;

            if (password == null)
            {
                OrchestratorController.Instance.Login(username);
            }
            else
            {
                OrchestratorController.Instance.Login(username, password);
            }
        }

        public void Login(string username, Action<bool, string> callback)
        {
            Login(username, null, callback);
        }

        public void Logout(Action<bool> callback)
        {
            Action<bool> fn = null;
            fn = (success) =>
            {
                callback?.Invoke(success);
                OrchestratorController.Instance.OnLogoutEvent -= fn;
            };

            OrchestratorController.Instance.OnLogoutEvent += fn;
            OrchestratorController.Instance.Logout();
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
                CurrentUser.Session = CurrentSession;

                callback?.Invoke(CurrentSession);
                OrchestratorController.Instance.OnJoinSessionEvent -= fn;
            };

            OrchestratorController.Instance.OnJoinSessionEvent += fn;
            OrchestratorController.Instance.JoinSession(sessionId);
        }
    }
}
