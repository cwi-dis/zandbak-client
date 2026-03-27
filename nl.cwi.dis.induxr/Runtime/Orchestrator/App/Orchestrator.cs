#pragma warning disable CS0618

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orchestrator.Data;
using Orchestrator.Wrapping;
using UnityEngine;
using DeviceType = Orchestrator.Wrapping.OrchestratorController.DeviceType;

namespace Orchestrator.App
{
    public class Orchestrator
    {
        /// <summary>
        /// The URL that was used for establishing a Socket.IO connection to the Orchestrator backend. This property
        /// will be null if we are currently not connected to the Orchestrator.
        /// </summary>
        /// <remarks>
        /// - The WebSocket URL is managed by the singleton instance of <c>OrchestratorController</c>.
        /// - This property is a direct reference to the <c>SocketUrl</c> of the <c>OrchestratorController</c>.
        /// </remarks>
        public Uri SocketUrl => OrchestratorController.Instance.SocketUrl;

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
        public Session CurrentSession { get; set; }

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
        public SelfUser Self { get; private set; }

        /// <summary>
        /// Occurs when a new session is created
        /// </summary>
        /// <remarks>
        /// This event is triggered whenever a new session is created. The event provides the session as an
        /// argument, allowing subscriber methods to access and process the data.
        /// </remarks>
        public event Action<Session> OnSessionCreated;

        /// <summary>
        /// Occurs when a new session is deleted
        /// </summary>
        /// <remarks>
        /// This event is triggered whenever a session is deleted. The event provides the ID of the deleted session as
        /// an argument.
        /// </remarks>
        public event Action<string> OnSessionDeleted;

        public Orchestrator()
        {
            OrchestratorController.Instance.OnSessionCreatedEvent += SessionCreated;
            OrchestratorController.Instance.OnSessionDeletedEvent += SessionDeleted;
        }

        /// <summary>
        /// Retrieves the version of the Orchestrator asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the orchestrator version as a string.</returns>
        public Task<string> GetOrchestratorVersion()
        {
            var tcs = new TaskCompletionSource<string>();

            OrchestratorController.Instance.Wrapper.GetOrchestratorVersion((_, version) =>
            {
                tcs.SetResult(version);
            });

            return tcs.Task;
        }

        /// <summary>
        /// Authenticates the user with the provided username and an optional password. If no password is given (or it
        /// is set to null), a passwordless login is attempted. If a password is provided and the server could not
        /// verify the user's identity, an exception is raised. The method also accepts a third parameter, allowing the
        /// caller to specify the user's device type. If not set, the parameter defaults to 'unknown'.
        /// </summary>
        /// <param name="username">The username of the user attempting to log in.</param>
        /// <param name="password">The password of the user, if required. Defaults to null for passwordless login.</param>
        /// <param name="deviceType">The type of the device that the user uses to log in. Defaults to "unknown".</param>
        /// <returns>A task representing the asynchronous operation. The task result contains the user ID as a string if the login was successful.</returns>
        public Task<User> Login(string username, string password = null, DeviceType deviceType = DeviceType.Unknown)
        {
            var tcs = new TaskCompletionSource<User>();

            Action<bool, Data.User> fn = null;
            fn = (success, userData) =>
            {
                if (success)
                {
                    Self = new SelfUser(this, userData);
                    tcs.SetResult(Self);
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
                Self = null;
                CurrentSession = null;

                tcs.SetResult(success);
                OrchestratorController.Instance.OnLogoutEvent -= fn;
            };

            OrchestratorController.Instance.OnLogoutEvent += fn;
            OrchestratorController.Instance.Logout();

            return tcs.Task;
        }

        /// <summary>
        /// Disconnect from the Orchestrator
        /// </summary>
        public void Disconnect()
        {
            OrchestratorController.Instance.Disconnect();
        }

        /// <summary>
        /// Retrieves the current Network Time Protocol (NTP) time asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the NTP time as a double.</returns>
        public Task<double> GetNtpTime()
        {
            var tcs = new TaskCompletionSource<double>();

            OrchestratorController.Instance.Wrapper.GetNtpTime((_, ntpTime) =>
            {
                tcs.SetResult(ntpTime.Timestamp);
            });

            return tcs.Task;
        }

        /// <summary>
        /// Retrieves the list of active sessions asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of sessions.</returns>
        public Task<List<Session>> GetSessions()
        {
            var tcs = new TaskCompletionSource<List<Session>>();

            OrchestratorController.Instance.Wrapper.GetSessions((_, sessions) =>
            {
                Sessions = sessions.Select(session => new Session(this, session)).ToList();
                tcs.SetResult(Sessions);
            });

            return tcs.Task;
        }

        /// <summary>
        /// Retrieves a list of scheduled sessions from the Orchestrator
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of scheduled sessions.</returns>
        public Task<List<ScheduledSession>> GetScheduledSessions()
        {
            var tcs = new TaskCompletionSource<List<ScheduledSession>>();

            OrchestratorController.Instance.Wrapper.GetScheduledSessions((_, scheduledSessions) =>
            {
                tcs.SetResult(scheduledSessions);
            });

            return tcs.Task;
        }

        /// <summary>
        /// Retrieves a list of available rooms from the Orchestrator
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of available rooms.</returns>
        public Task<List<Room>> GetRooms()
        {
            var tcs = new TaskCompletionSource<List<Room>>();

            OrchestratorController.Instance.Wrapper.GetRooms((_, body) =>
            {
                var rooms = body.Select(r => new Room(this, r)).ToList();
                tcs.SetResult(rooms);
            });

            return tcs.Task;
        }

        /// <summary>
        /// Creates a new session asynchronously with the specified session name.
        /// </summary>
        /// <param name="sessionName">The name of the session to be created.</param>
        /// <param name="room">The room model to use for this session</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created session object.</returns>
        public Task<Session> CreateSession(string sessionName, Room room)
        {
            var tcs = new TaskCompletionSource<Session>();

            OrchestratorController.Instance.Wrapper.AddSession(sessionName, "", "socketio", room.Id, new[] { "transform" }, false,
                (_, session) =>
                {
                    CurrentSession = new Session(this, session);
                    CurrentSession.Join();

                    tcs.SetResult(CurrentSession);
                });

            return tcs.Task;
        }

        /// <summary>
        /// Creates a new persistent session asynchronously with the specified session name.
        /// </summary>
        /// <param name="sessionName">The name of the session to be created.</param>
        /// <param name="room">The room model to use for this session</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created session object.</returns>
        public Task<Session> CreatePersistentSession(string sessionName, Room room)
        {
            var tcs = new TaskCompletionSource<Session>();

            OrchestratorController.Instance.Wrapper.AddSession(sessionName, "", "socketio", room.Id, new[] { "transform" }, true,
                (_, session) =>
                {
                    CurrentSession = new Session(this, session);
                    CurrentSession.Join();

                    tcs.SetResult(CurrentSession);
                });

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

            OrchestratorController.Instance.Wrapper.ScheduleSession(sessionId, (_, session) =>
            {
                CurrentSession = new Session(this, session);
                CurrentSession.Join();

                tcs.SetResult(CurrentSession);
            });

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
                    CurrentSession = sessionToJoin;
                    sessionToJoin.SessionData = session;
                    sessionToJoin.Join();

                    tcs.SetResult(sessionToJoin);
                    OrchestratorController.Instance.OnJoinSessionEvent -= fn;
                };

                OrchestratorController.Instance.OnJoinSessionEvent += fn;
                OrchestratorController.Instance.JoinSession(sessionId);
            }

            return tcs.Task;
        }

        /// <summary>
        /// Switches the current session to the specified session by its ID asynchronously.
        /// If no session is currently joined, it joins the specified session. If there is an active session, it leaves
        /// the current session before joining the new one. If a session with the specified ID is not found, an
        /// exception is raised.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the session to switch to.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the joined session.</returns>
        public async Task<Session> SwitchSessions(string sessionId)
        {
            if (CurrentSession == null)
                return await JoinSession(sessionId);

            await CurrentSession.Leave();
            CurrentSession = null;

            return await JoinSession(sessionId);
        }

        /// <summary>
        /// Searches for a session by its name in the current list of available sessions.
        /// </summary>
        /// <param name="name">The name of the session to find.</param>
        /// <returns>The session object with the matching name, or null if no session is found.</returns>
        /// <remarks>Refreshes the list of sessions before searching by calling GetSessions()</remarks>
        public async Task<Session> FindSessionByName(string name)
        {
            // Refresh the list of sessions
            await GetSessions();
            // Find the session by name
            return Sessions.Find((s) => s.Name == name);
        }

        private async void SessionCreated(Data.Session session)
        {
            var sessions = await GetSessions();
            var createdSession = sessions.Find((s) => s.Id == session.Id);

            if (createdSession != null)
            {
                OnSessionCreated?.Invoke(createdSession);
            }
        }

        private async void SessionDeleted(Data.Session session)
        {
            await GetSessions();
            OnSessionDeleted?.Invoke(session.Id);
        }
    }
}

#pragma warning restore CS0618
