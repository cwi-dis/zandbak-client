#pragma warning disable CS0618

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Orchestrator.Data;
using Orchestrator.Util;
using UnityEditor.PackageManager;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.Search;
#endif

namespace Orchestrator.Wrapping
{
    public class OrchestratorController : MonoBehaviour, IOrchestratorResponsesListener, IUserMessagesListener, IUserSessionEventsListener
    {
        [Tooltip("Enable trace logging output")]
        [SerializeField] private bool enableLogging = true;

        public enum OrchestratorConnectionStatus
        {
            Disconnected,
            Connecting,
            Connected
        }

        public enum DeviceType
        {
            VR,
            AR,
            Unknown
        }

        // the wrapper for the orchestrator
        private OrchestratorWrapper _orchestratorWrapper;
        // the reference controller for singleton
        private static OrchestratorController _instance;

        private OrchestratorConnectionStatus _connectionStatus;

        public App.Orchestrator Orchestrator { get; private set; }

        //Session
        private Session _session;
        private List<Session> _availableSessions = new();

        // user Login state
        private bool _userIsLogged;

        // user Login state
        private bool _userIsMaster;

        // orchestrator connection state
        private bool _connectedToOrchestrator;

        private TaskCompletionSource<App.Orchestrator> _connectionTaskCompletionSource = new();

        //Orchestrator Controller Singleton
        public static OrchestratorController Instance {
            get {
                if (_instance is null) {
                    Debug.LogError("OrchestratorController.Instance: No OrchestratorController yet");
                }
                return _instance;
            }
        }

        #region event handlers

        /// <summary>
        /// Invoked when an error occurs, with the error object as an argument
        /// </summary>
        public event Action<ResponseStatus> OnErrorEvent;

        /// <summary>
        /// Invoked whenever a new connection is attempted
        /// </summary>
        public event Action OnConnectingEvent;

        /// <summary>
        /// Invoked whenever a new connection is established
        /// </summary>
        public event Action<bool> OnConnectionEvent;

        /// <summary>
        /// Invoked when the Orchestrator's current version is requested, with the version as an argument
        /// </summary>
        public event Action<string> OnGetOrchestratorVersionEvent;

        /// <summary>
        /// Invoked when the current user logs into the Orchestrator, with a boolean indicating success and the user's ID as arguments
        /// </summary>
        public event Action<bool, User> OnLoginEvent;

        /// <summary>
        /// Invoked when the current user logs out of the Orchestrator, with a boolean indicating success as argument
        /// </summary>
        public event Action<bool> OnLogoutEvent;

        /// <summary>
        /// Invoked when the current NTP time is received, with the NTP time as argument
        /// </summary>
        public event Action<NtpClock> OnGetNtpTimeEvent;

        /// <summary>
        /// Invoked when a list of sessions has been requested with the list of sessions as argument
        /// </summary>
        public event Action<List<Session>> OnSessionsEvent;

        /// <summary>
        /// Invoked when a list of scheduled sessions has been requested with the list of scheduled sessions as argument
        /// </summary>
        public event Action<List<ScheduledSession>> OnScheduledSessionsEvent;

        /// <summary>
        /// Invoked when information about the current session has been requested, with the session information as argument
        /// </summary>
        public event Action<Session> OnSessionInfoEvent;

        /// <summary>
        /// Invoked when a new session has been created, with the session information as argument
        /// </summary>
        public event Action<Session> OnAddSessionEvent;


        /// <summary>
        /// Invoked when the current session is being closed remotely
        /// </summary>
        public event Action OnSessionCloseEvent;

        /// <summary>
        /// Invoked when a given session has been joined successfully, with the session information as argument
        /// </summary>
        public event Action<Session> OnJoinSessionEvent;

        /// <summary>
        /// Invoked in response to the current user in the session raising their hand
        /// </summary>
        public event Action OnRaisedHandEvent;

        /// <summary>
        /// Invoked in response to the current user clearing a raised hand
        /// </summary>
        public event Action OnClearRaisedHandEvent;

        /// <summary>
        /// Invoked in response to the current user requesting the list of raised hands. Receives a list of users as an argument
        /// </summary>
        public event Action<List<User>> OnGetRaisedHandsEvent;

        /// <summary>
        /// Invoked in response to the current user requesting the list of chat messages. Receives a list of chat messages as an argument
        /// </summary>
        public event Action<List<ChatMessage>> OnGetMessagesEvent;

        /// <summary>
        /// Invoked in response to the current user setting their isSpeaking flag
        /// </summary>
        public event Action<bool> OnIsSpeakingEvent;

        /// <summary>
        /// Invoked when a new user joins the current session, with the user ID as argument
        /// </summary>
        public event Action<string, User> OnUserJoinSessionEvent;

        /// <summary>
        /// Invoked when a user leaves the current session, with the user ID as argument
        /// </summary>
        public event Action<string> OnUserLeaveSessionEvent;

        /// <summary>
        /// Invoked when a user in the session raises their hand, with the user ID as argument
        /// </summary>
        public event Action<string> OnUserRaisedHandEvent;

        /// <summary>
        /// Invoked when a user's raised hand is cleared in the current session, with the user ID as argument
        /// </summary>
        public event Action<string> OnUserClearedRaisedHandEvent;

        /// <summary>
        /// Invoked when the status of the current session changes
        /// </summary>
        public event Action<string> OnSessionStatusChangedEvent;

        /// <summary>
        /// Invoked when the status of a user changes
        /// </summary>
        public event Action<User, string> OnUserStatusChangedEvent;

        /// <summary>
        /// Invoked when the current presentation of the current session changes
        /// </summary>
        public event Action<Presentation> OnSessionPresentationChangedEvent;

        /// <summary>
        /// Invoked when the current presentation's slide of the current session changes
        /// </summary>
        public event Action<Presentation> OnSessionPresentationSlideChangedEvent;

        /// <summary>
        /// Invoked when the current presentation's sharing status changes
        /// </summary>
        public event Action<Presentation> OnSessionPresentationIsSharingEvent;

        /// <summary>
        /// Invoked when a message is received in the current session
        /// </summary>
        public event Action<ChatMessage> OnUserMessageReceivedEvent;

        /// <summary>
        /// Invoked when a user changes their `isSpeaking` property
        /// </summary>
        public event Action<User, bool> OnSessionIsSpeakingEvent;

        /// <summary>
        /// Invoked when a broadcast is received in the current session
        /// </summary>
        public event Action<BroadcastData> OnBroadcastReceivedEvent;

        #endregion

        #region public properties

        public bool ConnectedToOrchestrator => _connectedToOrchestrator;
        public OrchestratorConnectionStatus ConnectionStatus => _connectionStatus;
        public bool UserIsLogged => _userIsLogged;
        public bool UserIsMaster => _userIsMaster;
        public User SelfUser { get; private set; }
        public Session[] AvailableSessions => _availableSessions?.ToArray();
        public Session CurrentSession => _session;

        #endregion

        #region Unity

        private void Awake() {
            if (_instance == null) {
                DontDestroyOnLoad(this.gameObject);
                this.gameObject.name += "_keep";
                _instance = this;
            } else if (_instance != this) {
#if UNITY_EDITOR
                string newName = SearchUtils.GetHierarchyPath(gameObject, false);
                string oldName = SearchUtils.GetHierarchyPath(_instance.gameObject, false);
#else
                string newName = gameObject.name;
                string oldName = _instance.gameObject.name;
#endif
                Debug.LogWarning($"OrchestratorController: attempt to create second instance from {newName}. Keep first one, from {oldName}.");
            }
        }

        private void OnDestroy() {
            Debug.Log($"{gameObject.name}: OrchestratorController.OnDestroy() called. Will close orchestrator connection. ");
            _orchestratorWrapper?.Disconnect();
        }

        #endregion

        #region Socket.io connect

        /// <summary>
        /// Establishes a socket connection to the Orchestrator using the specified URL.
        /// Invokes <c>OnConnectionEvent</c> upon completion.
        /// </summary>
        /// <param name="url">The URL of the orchestrator to establish the connection to.</param>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void SocketConnect(string url) {
            Log($"OrchestratorController: connect to {url}");

            _orchestratorWrapper = new OrchestratorWrapper(url, this, this, this);
            _orchestratorWrapper.Connect();
        }

        /// <summary>
        /// Establishes a socket connection to the Orchestrator using the specified URL in an asynchronous manner.
        /// Returns a Task, which returns an instance of an Orchestrator object upon successful connection.
        /// </summary>
        /// <param name="url">The URL of the orchestrator to establish the connection to.</param>
        public Task<App.Orchestrator> SocketConnectAsync(string url)
        {
            SocketConnect(url);
            return _connectionTaskCompletionSource.Task;
        }

        void IOrchestratorResponsesListener.OnConnect()
        {
            Log($"OrchestratorController: connected to orchestrator");

            #if UNITY_EDITOR
            // Verify that we're connected to a version of the Orchestrator that's compatible with this library
            VerifyOrchestratorVersion();
            #endif

            _connectedToOrchestrator = true;
            _connectionStatus = OrchestratorConnectionStatus.Connected;

            Orchestrator = new App.Orchestrator();

            OnConnectionEvent?.Invoke(true);
            _connectionTaskCompletionSource.SetResult(Orchestrator);
        }

        void IOrchestratorResponsesListener.OnConnecting() {
            Log($"OrchestratorController: connecting to orchestrator");

            _connectionStatus = OrchestratorConnectionStatus.Connecting;
            OnConnectingEvent?.Invoke();
        }

        /// <summary>
        /// Abort connection to Orchestrator.
        /// </summary>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void Abort() {
            _orchestratorWrapper.Disconnect();
            ((IOrchestratorResponsesListener)this).OnDisconnect();
        }

        /// <summary>
        /// Disconnect from the Orchestrator
        /// </summary>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void Disconnect()
        {
            Abort();
        }

        /// <summary>
        /// Retrieves the version of the Orchestrator by sending a request to the connected server.
        /// Invokes <c>OnGetOrchestratorVersionEvent</c> upon completion.
        /// </summary>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void GetVersion()
        {
            _orchestratorWrapper.GetOrchestratorVersion();
        }

        void IOrchestratorResponsesListener.OnGetOrchestratorVersionResponse(ResponseStatus status, string version) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            OnGetOrchestratorVersionEvent?.Invoke(version);
        }

        void IOrchestratorResponsesListener.OnDisconnect() {
            Debug.LogWarning($"OrchestratorController: disconnected from orchestrator");
            SelfUser = null;
            _connectedToOrchestrator = false;
            _connectionStatus = OrchestratorConnectionStatus.Disconnected;
            _userIsLogged = false;
            OnConnectionEvent?.Invoke(false);
        }

        #endregion

        #region Login/Logout

        /// <summary>
        /// Logs in a user to the Orchestrator with the specified username and the given device type.
        ///
        /// Invokes <c>OnLoginEvent</c> upon completion with a boolean parameter indicating whether the login was
        /// successful and if so, also a string parameter with the user's ID.
        /// </summary>
        /// <param name="username">The username of the user to log in.</param>
        /// <param name="deviceType">The type of device that the user uses to log in</param>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void Login(string username, DeviceType deviceType)
        {
            SelfUser = new User
            {
                Username = username,
                DeviceType = DeviceTypeToString(deviceType)
            };

            _orchestratorWrapper.Login(username, DeviceTypeToString(deviceType));
        }

        /// <summary>
        /// Logs in a user to the Orchestrator with the specified username, password and device type. The given
        /// username and password combination is checked against the Orchestrator's database.
        ///
        /// Invokes <c>OnLoginEvent</c> upon completion with a boolean parameter indicating whether the login was
        /// successful and if so, also a string parameter with the user's ID.
        /// </summary>
        /// <param name="username">The username of the user to log in.</param>
        /// <param name="password">The password of the user to log in</param>
        /// <param name="deviceType">The deviceType that the user uses to log in</param>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void Login(string username, string password, DeviceType deviceType)
        {
            SelfUser = new User
            {
                Username = username,
                Password = password,
                DeviceType = DeviceTypeToString(deviceType)
            };

            _orchestratorWrapper.Login(username, password, DeviceTypeToString(deviceType));
        }

        void IOrchestratorResponsesListener.OnLoginResponse(ResponseStatus status, User userData) {
            var userLoggedSuccessfully = (status.Error == 0);

            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            if (!_userIsLogged) {
                // user was not logged before the request
                if (userLoggedSuccessfully) {
                    Log("OrchestratorController: OnLoginResponse: User logged in.");

                    _userIsLogged = true;
                    SelfUser.Id = userData.Id;
                } else {
                    _userIsLogged = false;
                }
            } else {
                // user was logged in previously
                if (!userLoggedSuccessfully) {
                    // normal, user previously logged, nothing to do
                } else {
                    // should not occur
                }
            }

            OnLoginEvent?.Invoke(userLoggedSuccessfully, userData);
        }

        private static string DeviceTypeToString(DeviceType deviceType) => deviceType switch
        {
            DeviceType.VR => "vr",
            DeviceType.AR => "ar",
            _ => "unknown"
        };

        /// <summary>
        /// Terminates an existing Orchestrator connection.
        /// Invokes <c>OnLogoutEvent</c> upon completion.
        /// </summary>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void Logout() {
            _orchestratorWrapper.Logout();
        }

        void IOrchestratorResponsesListener.OnLogoutResponse(ResponseStatus status) {
            bool userLoggedOutSuccessfully = (status.Error == 0);

            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            SelfUser = null;
            _userIsLogged = false;

            OnLogoutEvent?.Invoke(userLoggedOutSuccessfully);
        }

        #endregion

        #region NTP clock

        /// <summary>
        /// Gets the current NTP time from the Orchestrator.
        /// Invokes <c>OnGetNtpTimeEvent</c> upon completion with the current NTP time.
        /// </summary>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void GetNtpTime() {
            _orchestratorWrapper.GetNtpTime();
        }

        void IOrchestratorResponsesListener.OnGetNTPTimeResponse(ResponseStatus status, NtpClock ntpTime) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            Log("OrchestratorController: OnGetNTPTimeResponse: NtpTime: " + ntpTime.Timestamp);
            if (OnGetNtpTimeEvent == null) Debug.LogWarning("OrchestratorController: NTP time response received but nothing listens");

            OnGetNtpTimeEvent?.Invoke(ntpTime);
        }

        #endregion

        #region Sessions

        /// <summary>
        /// Retrieves the list of currently active sessions.
        /// Invokes <c>OnSessionsEvent</c> upon completion with a list of sessions.
        /// </summary>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void GetSessions() {
            _orchestratorWrapper.GetSessions();
        }

        void IOrchestratorResponsesListener.OnGetSessionsResponse(ResponseStatus status, List<Session> sessions) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            int nRemoved = sessions.RemoveAll(item => item == null);

            if (nRemoved > 0)
            {
                Debug.LogWarning($"OrchestratorController: Removed {nRemoved} null sessions");
            }

            Log("OrchestratorController: OnGetSessionsResponse: Number of available sessions:" + sessions.Count);

            // update the list of available sessions
            _availableSessions = sessions;

            OnSessionsEvent?.Invoke(sessions);
        }

        /// <summary>
        /// Retrieves the list of scheduled sessions.
        /// Invokes <c>OnScheduledSessionsEvent</c> upon completion with a list of sessions.
        /// </summary>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void GetScheduledSessions()
        {
            _orchestratorWrapper.GetScheduledSessions();
        }

        void IOrchestratorResponsesListener.OnGetScheduledSessionsResponse(ResponseStatus status, List<ScheduledSession> sessions)
        {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            OnScheduledSessionsEvent?.Invoke(sessions);
        }

        /// <summary>
        /// Creates a new session with the given name and the given description.
        /// Invokes <c>OnAddSessionEvent</c> upon completion with all information about the created session.
        /// </summary>
        /// <param name="sessionName">The name of the session to be created</param>
        /// <param name="sessionDescription">The description of the session to be created. This parameter is optional and defaults to the empty string</param>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void AddSession(string sessionName, string sessionDescription = "") {
            _orchestratorWrapper.AddSession(sessionName, sessionDescription, "socketio", new[] { "transform" });
        }

        /// <summary>
        /// Creates a new session from a session stored in the Orchestrator's database identified by a session ID.
        /// Invokes <c>OnAddSessionEvent</c> upon completion with all information about the created session. The session
        /// with the given ID must exist in the Orchestrator's database, if no such session is found, an error is
        /// triggered.
        /// </summary>
        /// <param name="sessionId">The ID of the session to be created</param>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void ScheduleSession(string sessionId)
        {
            _orchestratorWrapper.ScheduleSession(sessionId);
        }

        void IOrchestratorResponsesListener.OnAddSessionResponse(ResponseStatus status, Session session) {
            if (status.Error != 0) {
                _session = null;
                OnErrorEvent?.Invoke(status);
                return;
            }

            Log("OrchestratorController: OnAddSessionResponse: Session " + session.Name + " successfully created by " + session.GetUser(session.AdministratorId).Username + ".");

            // success
            _session = session;

            _userIsMaster = session.MasterId == SelfUser.Id;

            _availableSessions.Add(session);
            OnAddSessionEvent?.Invoke(session);
        }

        /// <summary>
        /// Retrieves information about the session that the user is currently a member of.
        /// Invokes <c>OnSessionInfoEvent</c> upon completion with all information about the current session.
        /// </summary>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void GetSessionInfo() {
            _orchestratorWrapper.GetSessionInfo();
        }

        void IOrchestratorResponsesListener.OnGetSessionInfoResponse(ResponseStatus status, Session session) {
            if (_session == null || string.IsNullOrEmpty(session.Id)) {
                LogWarning("OrchestratorController: OnGetSessionInfoResponse: Aborted, current session is null.");
                return;
            }

            if (status.Error != 0) {
                LogError($"OrchestratorController: OnGetSessionInfoResponse: clear session, status={status}");
                _session = null;
                OnErrorEvent?.Invoke(status);
                return;
            }

            // success
            _session = session;
            _userIsMaster = session.MasterId == SelfUser.Id;
            int userCount = _session.GetUserCount();

            Log($"OrchestratorController: OnGetSessionInfoResponse: Get session info of {session.Name}, isMaster={(_userIsMaster)}, nUser={userCount}");

            OnSessionInfoEvent?.Invoke(session);
        }

        /// <summary>
        /// Deletes the current session. The current user must either be the session creator or its admin to be allowed
        /// to do this.
        /// </summary>
        /// <param name="pSessionID"></param>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void DeleteSession(string pSessionID) {
            _orchestratorWrapper.DeleteSession(pSessionID);
        }

        void IOrchestratorResponsesListener.OnDeleteSessionResponse(ResponseStatus status) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            Log("OrchestratorController: OnDeleteSessionResponse: Session successfully deleted.");

            _session = null;

            // update the lists of session, anyway the result
            _orchestratorWrapper.GetSessions();
        }

        /// <summary>
        /// Joins the session given by the ID.
        /// Invokes <c>OnJoinSessionEvent</c> upon completion with all information about the joined session.
        /// </summary>
        /// <param name="pSessionID">The ID of the session to be joined</param>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void JoinSession(string pSessionID) {
            _orchestratorWrapper.JoinSession(pSessionID);
        }

        void IOrchestratorResponsesListener.OnJoinSessionResponse(ResponseStatus status, Session session) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            // success
            _session = session;
            _userIsMaster = session.MasterId == SelfUser.Id;
            int userCount = session.GetUserCount();

            Log($"OrchestratorController: OnJoinSessionResponse: Session {session.Name}, isMaster={(_userIsMaster)}, nUser={userCount}");

            // Simulate a user joining a session for each connected user
            foreach (var id in session.UserIds) {
                if (id != SelfUser.Id) {
                    ((IUserSessionEventsListener)this).OnUserJoinedSession(id, null);
                }
            }

            OnJoinSessionEvent?.Invoke(_session);
        }

        /// <summary>
        /// Sets the current user's <c>isSpeaking</c> flag to the given value.
        /// Invokes <c>OnIsSpeakingEvent</c> upon completion.
        /// </summary>
        /// <param name="isSpeaking">The value to set the flag to</param>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void IsSpeaking(bool isSpeaking)
        {
            _orchestratorWrapper.IsSpeaking(isSpeaking);
        }

        void IOrchestratorResponsesListener.OnIsSpeakingResponse(ResponseStatus status)
        {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            OnIsSpeakingEvent?.Invoke(true);
        }

        /// <summary>
        /// Leaves the current session.
        /// </summary>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void LeaveSession() {
            _orchestratorWrapper.LeaveSession();
            _session = null;
        }

        /// <summary>
        /// Removes the user with the given ID from the current session. This action can only be performed by the
        /// session creator or its admin. Further, the given user must be in the same session as the current user.
        /// </summary>
        /// <param name="userId">ID of the user to remove from the session</param>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void RemoveUserFromSession(string userId)
        {
            _orchestratorWrapper.LeaveSession(userId);
        }

        /// <summary>
        /// Advances the current presentation to the next slide.
        /// This method triggers the associated functionality in the OrchestratorWrapper.
        /// </summary>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void GoToNextPresentation()
        {
            _orchestratorWrapper.GoToNextPresentation();
        }

        void IOrchestratorResponsesListener.OnGoToNextPresentationResponse(ResponseStatus status, Presentation presentation)
        {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
            }
        }

        /// <summary>
        /// Changes the current slide in the current session presentation by the specified offset.
        /// </summary>
        /// <param name="slideOffset">The offset to apply to the current slide. A positive value moves forward, a negative value moves backward.</param>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void ChangeSlide(int slideOffset)
        {
            _orchestratorWrapper.ChangeSlide(slideOffset);
        }

        void IOrchestratorResponsesListener.OnChangeSlideResponse(ResponseStatus status, Presentation presentation)
        {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
            }
        }

        /// <summary>
        /// Sets whether the current presentation is being shared.
        /// Updates the orchestrator with the sharing status. If the invoking user is not a presenter or administrator,
        /// an error is issued. If there is no current presentation, an error is issued as well. Upon success, all
        /// users will receive a session update with the updated presentation.
        /// </summary>
        /// <param name="isSharing">A boolean indicating if the current presentation should be marked as sharing.</param>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void SetCurrentPresentationIsSharing(bool isSharing)
        {
            _orchestratorWrapper.CurrentPresentationIsSharing(isSharing);
        }

        void IOrchestratorResponsesListener.OnCurrentPresentationIsSharingResponse(ResponseStatus status, Presentation presentation)
        {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
            }
        }

        /// <summary>
        /// Updates the current session's status using the specified status string.
        /// </summary>
        /// <param name="status">The new status to be applied to the session.</param>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void ChangeSessionStatus(string status)
        {
            _orchestratorWrapper.SetSessionStatus(status);
        }

        void IOrchestratorResponsesListener.OnChangeStatusResponse(ResponseStatus status, string sessionStatus)
        {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
            }
        }

        /// <summary>
        /// Changes the user's status to the specified value.
        /// </summary>
        /// <param name="status">The new status to assign to the user.</param>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void ChangeUserStatus(string status)
        {
            _orchestratorWrapper.SetUserStatus(status);
        }

        void IOrchestratorResponsesListener.OnChangeUserStatusResponse(ResponseStatus status, string userStatus)
        {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
            }
        }

        void IOrchestratorResponsesListener.OnLeaveSessionResponse(ResponseStatus status) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            Log("OrchestratorController: OnLeaveSessionResponse: Session successfully left.");

            // Set this at the end and for the session creator, when the session has been deleted.
            _session = null;
        }

        void IUserSessionEventsListener.OnSessionClosed()
        {
            // The session has been closed by the session creator.
            Log("OrchestratorController: OnSessionClosed: Current session closed by session creator.");

            OnSessionCloseEvent?.Invoke();

            // Leave the session
            LeaveSession();
        }

        void IUserSessionEventsListener.OnUserJoinedSession(string userID, User user) {
            // Someone has joined the session
            if (string.IsNullOrEmpty(userID))
            {
                Debug.LogError("OrchestratorController: OnUserJoinedSession: empty userID");
            }

            if (user == null)
            {
                user = _session.GetUser(userID);

                if (user == null)
                {
                    Debug.LogError($"OrchestratorController: OnUserJoinedSession: userID {userID} unknown");
                    return;
                }
            }

            Log("OrchestratorController: OnUserJoinedSession: User " + user.Username + " joined the session.");

            _orchestratorWrapper.GetSessionInfo();
            OnUserJoinSessionEvent?.Invoke(userID, user);
        }

        void IUserSessionEventsListener.OnUserLeftSession(string userID) {
            if (!string.IsNullOrEmpty(userID)) {
                _orchestratorWrapper.GetSessionInfo();
                OnUserLeaveSessionEvent?.Invoke(userID);
            }
        }

        void IUserSessionEventsListener.OnUserRaisedHand(string userID)
        {
            _orchestratorWrapper.GetSessionInfo();
            OnUserRaisedHandEvent?.Invoke(userID);
        }

        void IUserSessionEventsListener.OnUserClearedRaisedHand(string userID)
        {
            _orchestratorWrapper.GetSessionInfo();
            OnUserClearedRaisedHandEvent?.Invoke(userID);
        }

        void IUserSessionEventsListener.OnSessionStatusChanged(string status)
        {
            _orchestratorWrapper.GetSessionInfo();
            OnSessionStatusChangedEvent?.Invoke(status);
        }

        void IUserSessionEventsListener.OnPresentationChanged(Presentation presentation)
        {
            _orchestratorWrapper.GetSessionInfo();
            OnSessionPresentationChangedEvent?.Invoke(presentation);
        }

        void IUserSessionEventsListener.OnPresentationIsSharingChanged(Presentation presentation)
        {
            _orchestratorWrapper.GetSessionInfo();
            OnSessionPresentationIsSharingEvent?.Invoke(presentation);
        }

        void IUserSessionEventsListener.OnSlideChanged(Presentation presentation)
        {
            _orchestratorWrapper.GetSessionInfo();
            OnSessionPresentationSlideChangedEvent?.Invoke(presentation);
        }

        void IUserSessionEventsListener.OnSessionIsSpeakingChanged(string userId, bool isSpeaking)
        {
            var user = _session.UserDefinitions.Find((u) => u.Id == userId);
            if (user == null) return;

            user.IsSpeaking = isSpeaking;
            OnSessionIsSpeakingEvent?.Invoke(user, isSpeaking);
        }

        void IUserSessionEventsListener.OnUserStatusChanged(string userId, string status)
        {
            var user = _session.UserDefinitions.Find((u) => u.Id == userId);
            if (user == null) return;

            user.Status = status;
            OnUserStatusChangedEvent?.Invoke(user, status);
        }

        #endregion

        #region Messages

        /// <summary>
        /// Raises the current user's hand in the session.
        /// </summary>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void RaiseHand()
        {
            _orchestratorWrapper.RaiseHand();
        }

        void IOrchestratorResponsesListener.OnRaiseHandResponse(ResponseStatus status)
        {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            OnRaisedHandEvent?.Invoke();
        }

        /// <summary>
        /// Clears a user's raised hand, identified by the given user ID. Users can only clear their own raised hand.
        /// Admins and presenters can clear anyone's raised hands.
        /// </summary>
        /// <param name="userId">The ID of the user whose raised hand shall be cleared</param>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void ClearRaisedHand(string userId)
        {
            _orchestratorWrapper.ClearRaisedHand(userId);
        }

        /// <summary>
        /// Clears the current user's raised hand.
        /// </summary>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void ClearRaisedHand()
        {
            _orchestratorWrapper.ClearRaisedHand();
        }

        void IOrchestratorResponsesListener.OnClearRaisedHandResponse(ResponseStatus status)
        {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            OnClearRaisedHandEvent?.Invoke();
        }

        /// <summary>
        /// Retrieves the list of users who have raised their hands in the session.
        /// Invokes the <c>OnGetRaisedHandsEvent</c> event upon completion with the list of users.
        /// </summary>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void GetRaisedHands()
        {
            _orchestratorWrapper.GetRaisedHands();
        }

        void IOrchestratorResponsesListener.OnGetRaisedHandsResponse(ResponseStatus status, List<User> users)
        {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            OnGetRaisedHandsEvent?.Invoke(users);
        }

        /// <summary>
        /// Sends a message to the user identified by the given ID.
        /// </summary>
        /// <param name="pMessage">The message to be delivered</param>
        /// <param name="pUserID">The ID of the user that the message should be delivered to</param>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void SendMessage(string pMessage, string pUserID) {
            _orchestratorWrapper.SendMessage(pMessage, pUserID);
        }

        void IOrchestratorResponsesListener.OnSendMessageResponse(ResponseStatus status) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
            }
        }

        /// <summary>
        /// Sends a message to all users in the current session.
        /// </summary>
        /// <param name="pMessage">The message to be delivered</param>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void SendMessageToAll(string pMessage) {
            _orchestratorWrapper.SendMessageToAll(pMessage);
        }

        void IOrchestratorResponsesListener.OnSendMessageToAllResponse(ResponseStatus status) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
            }
        }

        void IUserMessagesListener.OnUserMessageReceived(ChatMessage userMessage) {
            OnUserMessageReceivedEvent?.Invoke(userMessage);
        }

        /// <summary>
        /// Retrieves the recent chat messages from the orchestrator.
        /// </summary>
        /// <param name="count">The number of messages to retrieve.</param>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void GetMessages(int count)
        {
            _orchestratorWrapper.GetMessages(count);
        }

        /// <summary>
        /// Retrieves all chat messages from the orchestrator.
        /// </summary>
        [Obsolete("Direct usage of OrchestratorController is deprecated. Use the instance of App.Orchestrator returned by SocketConnectAsync() instead")]
        public void GetMessages()
        {
            _orchestratorWrapper.GetMessages();
        }

        void IOrchestratorResponsesListener.OnGetMessagesResponse(ResponseStatus status, List<ChatMessage> messages)
        {
            OnGetMessagesEvent?.Invoke(messages);
        }

        #endregion

        #region Events

        /// <summary>
        /// Broadcasts some data to all users in the current session listening on the given channel.
        /// </summary>
        /// <param name="channel">Channel to broadcast the message to</param>
        /// <param name="data">Data to be broadcast</param>
        public void Broadcast(string channel, string data)
        {
            byte[] lData = Encoding.ASCII.GetBytes(data);
            _orchestratorWrapper.SendBroadcastToChannel(channel, lData);
        }

        void IUserMessagesListener.OnBroadcastReceived(BroadcastData broadcastData) {
            OnBroadcastReceivedEvent?.Invoke(broadcastData);
        }

        #endregion

        #region Errors

        void IOrchestratorResponsesListener.OnError(ResponseStatus status) {
            Log("OrchestratorController: OnError: Error code: " + status.Error + "::Error message: " + status.Message);

            OnErrorEvent?.Invoke(status);
        }

        #endregion

        private void Log(string message)
        {
            if (enableLogging)
            {
                Debug.Log(message);
            }
        }

        private void LogError(string message)
        {
            if (enableLogging)
            {
                Debug.LogError(message);
            }
        }

        private void LogWarning(string message)
        {
            if (enableLogging)
            {
                Debug.LogWarning(message);
            }
        }

        #if UNITY_EDITOR
        private void VerifyOrchestratorVersion()
        {
            // Get package info for this package
            var packageInfo = PackageInfo.FindForPackageName("nl.cwi.dis.induxr");

            if (packageInfo != null)
            {
                // Lambda function to be called in response to the orchestrator version event
                Action<string> fn = null;
                fn = (version) =>
                {
                    try
                    {
                        // Create instances of SemanticVersion with the version strings, so they can be compared
                        var packageVersion = new SemanticVersion(packageInfo.version);
                        var orchestratorVersion = new SemanticVersion(version);

                        // Log warning if versions do not match
                        if (packageVersion != orchestratorVersion)
                        {
                            // Print different warning depending on which version is greater
                            Debug.LogWarning(packageVersion > orchestratorVersion
                                ? $"The OrchestratorWrapper package (v{packageVersion}) is newer than the connected Orchestrator (v{orchestratorVersion}). Compatibility is not guaranteed. Please update the Orchestrator!"
                                : $"The OrchestratorWrapper package (v{packageVersion}) is older than the connected Orchestrator (v{orchestratorVersion}). Compatibility is not guaranteed. Please update this package!");
                        }
                    }
                    catch (SemanticVersionParseException e)
                    {
                        Debug.LogError(e.Message);
                    }

                    // Remove handler
                    OnGetOrchestratorVersionEvent -= fn;
                };

                // Attach a handler to the orchestrator version response event
                OnGetOrchestratorVersionEvent += fn;
                // Get Orchestrator version
                GetVersion();
            }
            else
            {
                Debug.LogWarning("Could not get version info from package");
            }
        }
        #endif
    }
}

#pragma warning restore CS0618
