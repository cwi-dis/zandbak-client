using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orchestrator.Wrapping;
using UnityEngine;

namespace Orchestrator.App
{
    public class Orchestrator
    {
        /// <summary>
        /// A collection of available sessions within the Orchestrator. Keep in mind that this may not be up to date.
        /// To get the latest list of sessions, call <c>GetSessions()</c> instead.
        /// </summary>
        /// <remarks>
        /// - This property holds a list of sessions that are currently available.
        /// - The list is populated and updated through specific orchestrator operations, such as retrieving sessions from the backend.
        /// - Sessions in this list represent ongoing or joinable activities within the Orchestrator.
        /// </remarks>
        public List<Session> Sessions { get; private set; }

        /// <summary>
        /// Represents the session that the user is currently a member of. This property provides access to the session
        /// that the user has joined or created. If no session has been joined yet, or the user left their session, this
        /// property will be null.
        /// </summary>
        /// <remarks>
        /// - This property is updated whenever the user joins or creates a session.
        /// - The value will be null if no session is currently active.
        /// - Use methods such as <c>CreateSession(string sessionName)</c> or <c>JoinSession(string sessionId)</c>
        /// to populate or update the active session.
        /// - The <c>CurrentSession</c> provides access to session-specific details such as its ID, name, users,
        /// and events like user joining or leaving.
        /// </remarks>
        public Session CurrentSession { get; private set; }

        /// <summary>
        /// Represents the currently logged-in user in the Orchestrator.
        /// This property is dynamically updated upon login and null upon logout.
        /// </summary>
        /// <remarks>
        /// - This property provides information about the user that is currently authenticated in the application.
        /// - The <c>Self</c> object is initialized during a successful login and may include the user's session information.
        /// - It is essential for performing user-related operations within the Orchestrator, such as tracking the active session or user identity.
        /// - If no user is authenticated, <c>Self</c> will be null.
        /// </remarks>
        public User Self { get; private set; }

        /// <summary>
        /// Retrieves the version of the Orchestrator asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the orchestrator version as a string.</returns>
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

        /// <summary>
        /// Authenticates the user with the provided username and an optional password. If no password is given, a
        /// passwordless login is attempted. If a password is provided and the server could not verify the user's
        /// identity, an exception is raised.
        /// </summary>
        /// <param name="username">The username of the user attempting to log in.</param>
        /// <param name="password">The password of the user, if required. Defaults to null for passwordless login.</param>
        /// <param name="deviceType">The type of the device that the user uses to log in. Defaults to "unknown".</param>
        /// <returns>A task representing the asynchronous operation. The task result contains the user ID as a string if the login was successful.</returns>
        public Task<string> Login(string username, string password = null, string deviceType = "unknown")
        {
            var tcs = new TaskCompletionSource<string>();

            Action<bool, string> fn = null;
            fn = (success, userId) =>
            {
                if (success)
                {
                    tcs.SetResult(userId);

                    Self = new User(
                        new Data.User
                        {
                            Username = username,
                            Id = userId,
                            DeviceType = deviceType
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
                OrchestratorController.Instance.Login(username, deviceType);
            }
            else
            {
                OrchestratorController.Instance.Login(username, password, deviceType);
            }

            return tcs.Task;
        }

        /// <summary>
        /// Logs the user out from the Orchestrator asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous logout operation. The task result is a boolean indicating whether the logout was successful.</returns>
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

        /// <summary>
        /// Retrieves the current Network Time Protocol (NTP) time asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the NTP time as a double.</returns>
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

        /// <summary>
        /// Retrieves the list of active sessions asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of sessions.</returns>
        public Task<List<Session>> GetSessions()
        {
            var tcs = new TaskCompletionSource<List<Session>>();

            Action<Data.Session[]> fn = null;
            fn = (sessions) =>
            {
                Sessions = sessions.Select(session => new Session(this, session)).ToList();
                tcs.SetResult(Sessions);
                OrchestratorController.Instance.OnSessionsEvent -= fn;
            };

            OrchestratorController.Instance.OnSessionsEvent += fn;
            OrchestratorController.Instance.GetSessions();

            return tcs.Task;
        }

        /// <summary>
        /// Creates a new session asynchronously with the specified session name.
        /// </summary>
        /// <param name="sessionName">The name of the session to be created.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created session object.</returns>
        public Task<Session> CreateSession(string sessionName)
        {
            var tcs = new TaskCompletionSource<Session>();

            Action<Data.Session> fn = null;
            fn = (session) =>
            {
                CurrentSession = new Session(this, session);
                Self.Session = CurrentSession;

                tcs.SetResult(CurrentSession);
                OrchestratorController.Instance.OnAddSessionEvent -= fn;
            };

            OrchestratorController.Instance.OnAddSessionEvent += fn;
            OrchestratorController.Instance.AddSession(sessionName);

            return tcs.Task;
        }

        /// <summary>
        /// Creates a new session asynchronously from a session stored in the Orchestrator's database using its session
        /// ID. The new session will contain the presentation schedule as stored in the database.
        /// </summary>
        /// <param name="sessionId">The ID of the session to be created.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created session object.</returns>
        public Task<Session> ScheduleSession(string sessionId)
        {
            var tcs = new TaskCompletionSource<Session>();

            Action<Data.Session> fn = null;
            fn = (session) =>
            {
                CurrentSession = new Session(this, session);
                Self.Session = CurrentSession;

                tcs.SetResult(CurrentSession);
                OrchestratorController.Instance.OnAddSessionEvent -= fn;
            };

            OrchestratorController.Instance.OnAddSessionEvent += fn;
            OrchestratorController.Instance.ScheduleSession(sessionId);

            return tcs.Task;
        }

        /// <summary>
        /// Joins the session with the specified ID asynchronously.
        /// </summary>
        /// <param name="sessionId">The ID of the session to be joined.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an object representing the joined session.</returns>
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
                    sessionToJoin.SessionData = session;
                    CurrentSession = sessionToJoin;
                    Self.Session = CurrentSession;

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
