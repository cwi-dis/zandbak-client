using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orchestrator.Wrapping;
using UnityEngine;

namespace Orchestrator.App
{
    public class Orchestrator : MonoBehaviour
    {
        public List<Session> Sessions { get; private set; }
        public Session CurrentSession { get; private set; }
        public User CurrentUser { get; private set; }

        public Task<string> GetOrchestratorVersion()
        {
            var tcs = new TaskCompletionSource<string>();

            Action<string> fn = null;
            fn = (version) =>
            {
                tcs.SetResult(version);
                OrchestratorController.Instance.OnGetOrchestratorVersionEvent -= fn;
            };

            OrchestratorController.Instance.OnGetOrchestratorVersionEvent += fn;
            OrchestratorController.Instance.GetVersion();

            return tcs.Task;
        }

        public Task<string> Login(string username, string password = null)
        {
            var tcs = new TaskCompletionSource<string>();

            Action<bool, string> fn = null;
            fn = (success, userId) =>
            {
                if (success)
                {
                    tcs.SetResult(userId);

                    CurrentUser = new User(
                        new Data.User
                        {
                            Username = username,
                            Id = userId
                        }
                    );
                }
                else
                {
                    tcs.SetException(new Exception("Login failed"));
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

            return tcs.Task;
        }

        public Task<bool> Logout()
        {
            var tcs = new TaskCompletionSource<bool>();

            Action<bool> fn = null;
            fn = (success) =>
            {
                tcs.SetResult(success);
                OrchestratorController.Instance.OnLogoutEvent -= fn;
            };

            OrchestratorController.Instance.OnLogoutEvent += fn;
            OrchestratorController.Instance.Logout();

            return tcs.Task;
        }

        public Task<double> GetNtpTime()
        {
            var tcs = new TaskCompletionSource<double>();

            Action<Data.NtpClock> fn = null;
            fn = (ntpTime) =>
            {
                tcs.SetResult(ntpTime.Timestamp);
                OrchestratorController.Instance.OnGetNtpTimeEvent -= fn;
            };

            OrchestratorController.Instance.OnGetNtpTimeEvent += fn;
            OrchestratorController.Instance.GetNtpTime();

            return tcs.Task;
        }

        public Task<List<Session>> GetSessions()
        {
            var tcs = new TaskCompletionSource<List<Session>>();

            Action<Data.Session[]> fn = null;
            fn = (sessions) =>
            {
                Sessions = sessions.Select(session => new Session(session)).ToList();
                tcs.SetResult(Sessions);
                OrchestratorController.Instance.OnSessionsEvent -= fn;
            };

            OrchestratorController.Instance.OnSessionsEvent += fn;
            OrchestratorController.Instance.GetSessions();

            return tcs.Task;
        }

        public Task<Session> CreateSession(string sessionName)
        {
            var tcs = new TaskCompletionSource<Session>();

            Action<Data.Session> fn = null;
            fn = (session) =>
            {
                CurrentSession = new Session(session);
                CurrentUser.Session = CurrentSession;

                tcs.SetResult(CurrentSession);
                OrchestratorController.Instance.OnAddSessionEvent -= fn;
            };

            OrchestratorController.Instance.OnAddSessionEvent += fn;
            OrchestratorController.Instance.AddSession(sessionName);

            return tcs.Task;
        }

        public Task<Session> JoinSession(string sessionId)
        {
            var tcs = new TaskCompletionSource<Session>();
            var sessionToJoin = Sessions.Find((s) => s.Id == sessionId);

            if (sessionToJoin == null)
            {
                Debug.LogError($"Session {sessionId} not found");
                tcs.SetException(new Exception($"Session {sessionId} not found"));
            }
            else
            {
                Action<Data.Session> fn = null;
                fn = (session) =>
                {
                    sessionToJoin.Update(session);
                    CurrentSession = sessionToJoin;
                    CurrentUser.Session = CurrentSession;

                    tcs.SetResult(CurrentSession);
                    OrchestratorController.Instance.OnJoinSessionEvent -= fn;
                };

                OrchestratorController.Instance.OnJoinSessionEvent += fn;
                OrchestratorController.Instance.JoinSession(sessionId);
            }

            return tcs.Task;
        }
    }
}
