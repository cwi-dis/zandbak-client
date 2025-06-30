using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Orchestrator.Data;

#if UNITY_EDITOR
using UnityEditor.Search;
#endif

namespace Orchestrator.Wrapping
{
    public class OrchestratorController : MonoBehaviour, IOrchestratorResponsesListener, IUserMessagesListener, IUserSessionEventsListener
    {
        [Tooltip("Enable trace logging output")]
        [SerializeField] private bool enableLogging = true;

        public enum OrchestratorConnectionStatus {
            Disconnected,
            Connecting,
            Connected
        }

        // the wrapper for the orchestrator
        private OrchestratorWrapper _orchestratorWrapper;
        // the reference controller for singleton
        private static OrchestratorController _instance;

        private OrchestratorConnectionStatus _connectionStatus;

        public App.Orchestrator Orchestrator { get; private set; }

        //Session
        private Session _session;
        private List<Session> _availableSessions = new List<Session>();

        // user Login state
        private bool _userIsLogged;

        // user Login state
        private bool _userIsMaster;

        // orchestrator connection state
        private bool _connectedToOrchestrator;
        private bool _hasBeenConnectedToOrchestrator;

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
        public event Action<bool, string> OnLoginEvent;
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
        public event Action<Session[]> OnSessionsEvent;
        /// <summary>
        /// Invoked when information about the current session has been requested, with the session information as argument
        /// </summary>
        public event Action<Session> OnSessionInfoEvent;
        /// <summary>
        /// Invoked when a new session has been created, with the session information as argument
        /// </summary>
        public event Action<Session> OnAddSessionEvent;
        /// <summary>
        /// Invoked when a given session has been joined successfully, with the session information as argument
        /// </summary>
        public event Action<Session> OnJoinSessionEvent;
        /// <summary>
        /// Invoked when a new user joins the current session, with the user ID as argument
        /// </summary>
        public event Action<string, User> OnUserJoinSessionEvent;
        /// <summary>
        /// Invoked when a user leaves the current session, with the user ID as argument
        /// </summary>
        public event Action<string> OnUserLeaveSessionEvent;
        /// <summary>
        /// Invoked when a user raises their hand in the current session, with the user ID as argument
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
        /// Invoked when the current presentation of the current session changes
        /// </summary>
        public event Action<Presentation> OnSessionPresentationChangedEvent;
        /// <summary>
        /// Invoked when the current presentation's slide of the current session changes
        /// </summary>
        public event Action<Presentation> OnSessionPresentationSlideChangedEvent;
        /// <summary>
        /// Invoked when a message is received in the current session
        /// </summary>
        public event Action<UserMessage> OnUserMessageReceivedEvent;
        // Orchestrator User Messages Events
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
                string oldName = instance.gameObject.name;
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

            _connectedToOrchestrator = true;
            _hasBeenConnectedToOrchestrator = true;
            _connectionStatus = OrchestratorConnectionStatus.Connected;

            Orchestrator = new App.Orchestrator();

            OnConnectionEvent?.Invoke(true);
            _connectionTaskCompletionSource.SetResult(Orchestrator);
        }

        void IOrchestratorResponsesListener.OnConnecting() {
            Log($"OrchestratorController: connecting to orchestrator");

            if (_hasBeenConnectedToOrchestrator)
            {
                Debug.LogWarning("OrchestratorController: attempting to reconnect to orchestrator");
            }

            _connectionStatus = OrchestratorConnectionStatus.Connecting;
            OnConnectingEvent?.Invoke();
        }

        /// <summary>
        /// Abort connection to Orchestrator.
        /// </summary>
        public void Abort() {
            _orchestratorWrapper.Disconnect();
            ((IOrchestratorResponsesListener)this).OnDisconnect();
        }

        /// <summary>
        /// Retrieves the version of the Orchestrator by sending a request to the connected server.
        /// Invokes <c>OnGetOrchestratorVersionEvent</c> upon completion.
        /// </summary>
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
        /// Logs in a user to the Orchestrator with the specified username.
        ///
        /// Invokes <c>OnLoginEvent</c> upon completion with a boolean parameter indicating whether the login was
        /// successful and if so, also a string parameter with the user's ID.
        /// </summary>
        /// <param name="pName">The username of the user to log in.</param>
        public void Login(string pName)
        {
            SelfUser = new User
            {
                Username = pName
            };

            _orchestratorWrapper.Login(pName);
        }

        /// <summary>
        /// Logs in a user to the Orchestrator with the specified username and password. The given username and password
        /// combination is checked against the Orchestrator's database.
        ///
        /// Invokes <c>OnLoginEvent</c> upon completion with a boolean parameter indicating whether the login was
        /// successful and if so, also a string parameter with the user's ID.
        /// </summary>
        /// <param name="username">The username of the user to log in.</param>
        /// <param name="password">The password of the user to log in</param>
        public void Login(string username, string password)
        {
            SelfUser = new User
            {
                Username = username,
                Password = password
            };

            _orchestratorWrapper.Login(username, password);
        }

        void IOrchestratorResponsesListener.OnLoginResponse(ResponseStatus status, string userId) {
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
                    SelfUser.Id = userId;
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

            OnLoginEvent?.Invoke(userLoggedSuccessfully, userId);
        }


        /// <summary>
        /// Terminates an existing Orchestrator connection.
        /// Invokes <c>OnLogoutEvent</c> upon completion.
        /// </summary>
        public void Logout() {
            _orchestratorWrapper.Logout();
        }

        void IOrchestratorResponsesListener.OnLogoutResponse(ResponseStatus status) {
            bool userLoggedOutSuccessfully = (status.Error == 0);

            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            if (!_userIsLogged) {
                //user was not logged before the request
                if (!userLoggedOutSuccessfully) {
                    // normal, was not logged, nothing to do
                } else {
                    // should not occur
                }
            } else {
                //user was logged before the request
                if (userLoggedOutSuccessfully) {
                    Log("OrchestratorController: OnLogoutResponse: User logout.");

                    //normal
                    SelfUser = null;
                    _userIsLogged = false;
                } else {
                    // problem while logout
                    _userIsLogged = true;
                }
            }

            OnLogoutEvent?.Invoke(userLoggedOutSuccessfully);
        }

        #endregion

        #region NTP clock

        /// <summary>
        /// Gets the current NTP time from the Orchestrator.
        /// Invokes <c>OnGetNtpTimeEvent</c> upon completion with the current NTP time.
        /// </summary>
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

            OnSessionsEvent?.Invoke(sessions.ToArray());
        }

        /// <summary>
        /// Creates a new session with the given name and the given description.
        /// Invokes <c>OnAddSessionEvent</c> upon completion with all information about the created session.
        /// </summary>
        /// <param name="sessionName">The name of the session to be created</param>
        /// <param name="sessionDescription">The description of the session to be created. This parameter is optional and defaults to the empty string</param>
        public void AddSession(string sessionName, string sessionDescription = "") {
            _orchestratorWrapper.AddSession(sessionName, sessionDescription, "socketio", new[] { "transform" });
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

            // We may need to update our own user definition (because the sfuData may have been added)
            User newMe = session.GetUser(SelfUser.Id);
            if (newMe == null)
            {
                Debug.LogError($"OrchestratorController: OnAddSessionResponse: userId {SelfUser.Id} (which is me) not in session");
                return;
            }

            SelfUser = newMe;
            _userIsMaster = session.MasterId == SelfUser.Id;

            _availableSessions.Add(session);
            OnAddSessionEvent?.Invoke(session);
        }

        /// <summary>
        /// Retrieves information about the session that the user is currently a member of.
        /// Invokes <c>OnSessionInfoEvent</c> upon completion with all information about the current session.
        /// </summary>
        public void GetSessionInfo() {
            _orchestratorWrapper.GetSessionInfo();
        }

        void IOrchestratorResponsesListener.OnGetSessionInfoResponse(ResponseStatus status, Session session) {
            if (_session == null || string.IsNullOrEmpty(session.Id)) {
                LogError("OrchestratorController: OnGetSessionInfoResponse: Aborted, current session is null.");
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
        /// Leaves the current session.
        /// </summary>
        public void LeaveSession() {
            _orchestratorWrapper.LeaveSession();
        }

        /// <summary>
        /// Advances the current presentation to the next slide.
        /// This method triggers the associated functionality in the OrchestratorWrapper.
        /// </summary>
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

        void IOrchestratorResponsesListener.OnLeaveSessionResponse(ResponseStatus status) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            Log("OrchestratorController: OnLeaveSessionResponse: Session " + _session.Name + " successfully left.");

            if (_session != null && SelfUser != null) {
                // As the session creator, the session should be deleted when leaving.
                if (_session.AdministratorId == SelfUser.Id) {
                    Log("OrchestratorController: OnLeaveSessionResponse: As session creator, delete the current session when its empty.");
                    StartCoroutine(WaitForEmptySessionToDelete());
                    return;
                }
            }

            // Set this at the end and for the session creator, when the session has been deleted.
            _session = null;
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
                // If the session creator left, I need to leave also.
                if (_session.AdministratorId == userID) {
                    Debug.Log("OrchestratorController: OnUserLeftSession: Session creator " + _session.GetUser(userID).Username + " left the session. Also leaving.");
                    LeaveSession();
                }
                // Otherwise, proceed to the common user left event.
                else {
                    Log("OrchestratorController: OnUserLeftSession: User " + _session.GetUser(userID).Username + " left the session. Getting new session info.");

                    // Required to update the list of connected users.
                    _orchestratorWrapper.GetSessionInfo();
                    OnUserLeaveSessionEvent?.Invoke(userID);
                }
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

        void IUserSessionEventsListener.OnSlideChanged(Presentation presentation)
        {
            _orchestratorWrapper.GetSessionInfo();
            OnSessionPresentationSlideChangedEvent?.Invoke(presentation);
        }

        #endregion

        #region Messages

        /// <summary>
        /// Raises the current user's hand in the session.
        /// </summary>
        public void RaiseHand()
        {
            _orchestratorWrapper.RaiseHand();
        }

        void IOrchestratorResponsesListener.OnRaiseHandResponse(ResponseStatus status)
        {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
            }
        }

        /// <summary>
        /// Sends a message to the user identified by the given ID.
        /// </summary>
        /// <param name="pMessage">The message to be delivered</param>
        /// <param name="pUserID">The ID of the user that the message should be delivered to</param>
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
        public void SendMessageToAll(string pMessage) {
            _orchestratorWrapper.SendMessageToAll(pMessage);
        }

        void IOrchestratorResponsesListener.OnSendMessageToAllResponse(ResponseStatus status) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
            }
        }

        void IUserMessagesListener.OnUserMessageReceived(UserMessage userMessage) {
            OnUserMessageReceivedEvent?.Invoke(userMessage);
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

        #region Logic

        private IEnumerator WaitForEmptySessionToDelete() {
            if (_session == null) {
                yield break;
            }

            // Check frequently if there are users connected and ensure a null session (from the delete command) is escaped.
            while (_session.UserIds.Length > 0) {
                GetSessionInfo();
                yield return new WaitForSeconds(1.0f);
            }

            // When the session is free of users, delete it.
            if (_session.UserIds.Length == 0) {
                DeleteSession(_session.Id);
            }
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
    }
}
