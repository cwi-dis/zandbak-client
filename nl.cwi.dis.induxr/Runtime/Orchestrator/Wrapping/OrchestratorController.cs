using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
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

        //Users
        private User _me; // Accessed via SelfUser

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

        #region public

        //Orchestrator Controller Singleton
        public static OrchestratorController Instance {
            get {
                if (_instance is null) {
                    Debug.LogError("OrchestratorController.Instance: No OrchestratorController yet");
                }
                return _instance;
            }
        }

        // Orchestrator Error Response Events
        public Action<ResponseStatus> OnErrorEvent;

        // Orchestrator Connection Events
        public Action<bool> OnConnectionEvent;
        public Action OnConnectingEvent;
        public Action<string> OnGetOrchestratorVersionEvent;

        // Orchestrator Messages Events
        public Action<string> OnOrchestratorRequestEvent;
        public Action<string> OnOrchestratorResponseEvent;

        // Orchestrator Login Events
        public Action<bool, string> OnLoginEvent;
        public Action<bool> OnLogoutEvent;

        // Orchestrator NTP clock Events
        public Action<NtpClock> OnGetNtpTimeEvent;

        // Orchestrator Sessions Events
        public Action<Session[]> OnSessionsEvent;
        public Action<Session> OnSessionInfoEvent;
        public Action<Session> OnAddSessionEvent;
        public Action<Session> OnJoinSessionEvent;
        public Action OnSessionJoinedEvent;
        public Action OnLeaveSessionEvent;
        public Action OnDeleteSessionEvent;
        public Action<string, User> OnUserJoinSessionEvent;
        public Action<string> OnUserLeaveSessionEvent;
        public Action<string> OnUserRaisedHandEvent;
        public Action<string> OnUserClearedRaisedHandEvent;

        // Orchestrator User Messages Events
        public Action<UserMessage> OnUserMessageReceivedEvent;

        // Orchestrator User Messages Events
        public Action<UserEvent> OnMasterEventReceivedEvent;
        public Action<UserEvent> OnUserEventReceivedEvent;
        public Action<BroadcastData> OnBroadcastReceivedEvent;

        // Orchestrator Accessors
        public void LocalUserSessionForDevelopmentTests()
        {
            _userIsMaster = true;
            _session = new Session()
            {
                scenarioId = "LocalDevelopmentTest",
                sessionId = "0000"
            };
        }

        public bool ConnectedToOrchestrator { get { return _connectedToOrchestrator; } }
        public OrchestratorConnectionStatus ConnectionStatus { get { return _connectionStatus; } }
        public bool UserIsLogged { get { return _userIsLogged; } }
        public bool UserIsMaster { get { return _userIsMaster; } }
        public User SelfUser { get { return _me; } set { _me = value; } }
        public Session[] AvailableSessions { get { return _availableSessions?.ToArray(); } }
        public Session CurrentSession { get { return _session; } }

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
                // xxxjack Destroy(gameObject);
            }
        }

        private void OnDestroy() {
            Debug.Log($"{gameObject.name}: OrchestratorController.OnDestroy() called. Will close orchestrator connection. ");
            _orchestratorWrapper?.Disconnect();
        }

        #endregion

        #region Socket.io connect

        // Connect to the orchestrator
        public void SocketConnect(string pUrl) {
            if (enableLogging) Debug.Log($"OrchestratorController: connect to {pUrl}");
            _orchestratorWrapper = new OrchestratorWrapper(pUrl, this, this, this);
            _orchestratorWrapper.Connect();
        }

        void IOrchestratorResponsesListener.OnConnect()
        {
            if (enableLogging) Debug.Log($"OrchestratorController: connected to orchestrator");

            _connectedToOrchestrator = true;
            _hasBeenConnectedToOrchestrator = true;
            _connectionStatus = OrchestratorConnectionStatus.Connected;
            OnConnectionEvent?.Invoke(true);
        }

        void IOrchestratorResponsesListener.OnConnecting() {
            if (enableLogging) Debug.Log($"OrchestratorController: connecting to orchestrator");

            if (_hasBeenConnectedToOrchestrator)
            {
                Debug.LogWarning("OrchestratorController: attempting to reconnect to orchestrator");
            }

            _connectionStatus = OrchestratorConnectionStatus.Connecting;
            OnConnectingEvent?.Invoke();
        }

        // Abort Socket connection
        public void Abort() {
            _orchestratorWrapper.Disconnect();
            ((IOrchestratorResponsesListener)this).OnDisconnect();
        }

        public void GetVersion()
        {
            _orchestratorWrapper.GetOrchestratorVersion();
        }

        // Get connected Orchestrator version
        void IOrchestratorResponsesListener.OnGetOrchestratorVersionResponse(ResponseStatus status, string version) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            OnGetOrchestratorVersionEvent?.Invoke(version);
        }

        // SockerDisconnect response callback
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

        public void Login(string pName) {
            SelfUser = new User();
            SelfUser.userName = pName;

            _orchestratorWrapper.Login(pName);
        }

        public void Login(string username, string password)
        {
            SelfUser = new User
            {
                userName = username,
                userPassword = password
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
                //user was not logged before request
                if (userLoggedSuccessfully) {
                    if (enableLogging) Debug.Log("OrchestratorController: OnLoginResponse: User logged in.");

                    _userIsLogged = true;
                    SelfUser.userId = userId;
                } else {
                    _userIsLogged = false;
                }
            } else {
                //user was logged before previously
                if (!userLoggedSuccessfully) {
                    // normal, user previopusly logged, nothing to do
                } else {
                    // should not occur
                }
            }

            OnLoginEvent?.Invoke(userLoggedSuccessfully, userId);
        }


        public void Logout() {
            _orchestratorWrapper.Logout();
        }

        void IOrchestratorResponsesListener.OnLogoutResponse(ResponseStatus status) {
            bool userLoggedOutSucessfully = (status.Error == 0);

            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            if (!_userIsLogged) {
                //user was not logged before request
                if (!userLoggedOutSucessfully) {
                    // normal, was not logged, nothing to do
                } else {
                    // should not occur
                }
            } else {
                //user was logged before request
                if (userLoggedOutSucessfully) {
                    if (enableLogging) Debug.Log("OrchestratorController: OnLogoutResponse: User logout.");

                    //normal
                    SelfUser = null;
                    _userIsLogged = false;
                } else {
                    // problem while logout
                    _userIsLogged = true;
                }
            }

            OnLogoutEvent?.Invoke(userLoggedOutSucessfully);
        }

        #endregion

        #region NTP clock

        public void GetNtpTime() {
            _orchestratorWrapper.GetNtpTime();
        }

        void IOrchestratorResponsesListener.OnGetNTPTimeResponse(ResponseStatus status, NtpClock ntpTime) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            if (enableLogging) Debug.Log("OrchestratorController: OnGetNTPTimeResponse: NtpTime: " + ntpTime.Timestamp);
            if (OnGetNtpTimeEvent == null) Debug.LogWarning("OrchestratorController: NTP time response received but nothing listens");

            OnGetNtpTimeEvent?.Invoke(ntpTime);
        }

        #endregion

        #region Sessions

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

            if (enableLogging) Debug.Log("OrchestratorController: OnGetSessionsResponse: Number of available sessions:" + sessions.Count);

            // update the list of available sessions
            _availableSessions = sessions;

            OnSessionsEvent?.Invoke(sessions.ToArray());


        }

        public void AddSession(string pSessionName) {
            var scenario = new Scenario();
            var channels = new[] {"transform"};

            _orchestratorWrapper.AddSession("", scenario, pSessionName, "", "socketio", channels);
        }

        void IOrchestratorResponsesListener.OnAddSessionResponse(ResponseStatus status, Session session) {
            if (status.Error != 0) {
                _session = null;
                OnErrorEvent?.Invoke(status);
                return;
            }

            if (enableLogging) Debug.Log("OrchestratorController: OnAddSessionResponse: Session " + session.sessionName + " successfully created by " + session.GetUser(session.sessionAdministrator).userName + ".");

            // success
            _session = session;

            // We may need to update our own user definition (because the sfuData may have been added)
            User newMe = session.GetUser(SelfUser.userId);
            if (newMe == null)
            {
                Debug.LogError($"OrchestratorController: OnAddSessionResponse: userId {SelfUser.userId} (which is me) not in session");
                return;
            }

            SelfUser = newMe;
            _userIsMaster = session.sessionMaster == SelfUser.userId;

            _availableSessions.Add(session);
            OnAddSessionEvent?.Invoke(session);
        }

        public void GetSessionInfo() {
            _orchestratorWrapper.GetSessionInfo();
        }

        void IOrchestratorResponsesListener.OnGetSessionInfoResponse(ResponseStatus status, Session session) {
            if (_session == null || string.IsNullOrEmpty(session.sessionId)) {
                if (enableLogging) Debug.LogError("OrchestratorController: OnGetSessionInfoResponse: Aborted, current session is null.");
                return;
            }

            if (status.Error != 0) {
                if (enableLogging) Debug.LogError($"OrchestratorController: OnGetSessionInfoResponse: clear session, status={status}");
                _session = null;
                OnErrorEvent?.Invoke(status);
                return;
            }

            // success
            _session = session;
            _userIsMaster = session.sessionMaster == SelfUser.userId;
            int userCount = _session.GetUserCount();

            if (enableLogging) Debug.Log($"OrchestratorController: OnGetSessionInfoResponse: Get session info of {session.sessionName}, isMaster={(_userIsMaster)}, nUser={userCount}");

            OnSessionInfoEvent?.Invoke(session);
        }

        public void DeleteSession(string pSessionID) {
            _orchestratorWrapper.DeleteSession(pSessionID);
        }

        void IOrchestratorResponsesListener.OnDeleteSessionResponse(ResponseStatus status) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            if (enableLogging) Debug.Log("OrchestratorController: OnDeleteSessionResponse: Session succesfully deleted.");

            OnDeleteSessionEvent?.Invoke();
            _session = null;

            // update the lists of session, anyway the result
            _orchestratorWrapper.GetSessions();
        }

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
            _userIsMaster = session.sessionMaster == SelfUser.userId;
            int userCount = session.GetUserCount();

            if (enableLogging) Debug.Log($"OrchestratorController: OnJoinSessionResponse: Session {session.sessionName}, isMaster={(_userIsMaster)}, nUser={userCount}");

            // Simulate user join a session for each connected users
            foreach (string id in session.sessionUsers) {
                if (id != SelfUser.userId) {
                    ((IUserSessionEventsListener)this).OnUserJoinedSession(id, null);
                }
            }

            OnJoinSessionEvent?.Invoke(_session);
            OnSessionJoinedEvent?.Invoke();
        }

        public void LeaveSession() {
            _orchestratorWrapper.LeaveSession();
        }

        void IOrchestratorResponsesListener.OnLeaveSessionResponse(ResponseStatus status) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            if (enableLogging) Debug.Log("OrchestratorController: OnLeaveSessionResponse: Session " + _session.sessionName + " succesfully left.");

            // success
            OnLeaveSessionEvent?.Invoke();

            if (_session != null && SelfUser != null) {
                // As the session creator, the session should be deleted when leaving.
                if (_session.sessionAdministrator == SelfUser.userId) {
                    if (enableLogging) Debug.Log("OrchestratorController: OnLeaveSessionResponse: As session creator, delete the current session when its empty.");
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
            else
            {
                // xxxjack we don't add the user, but we call GetSessionInfo below to get a complete picture.
            }

            if (enableLogging) Debug.Log("OrchestratorController: OnUserJoinedSession: User " + user.userName + " joined the session.");

            _orchestratorWrapper.GetSessionInfo();
            OnUserJoinSessionEvent?.Invoke(userID, user);
        }

        void IUserSessionEventsListener.OnUserLeftSession(string userID) {
            if (!string.IsNullOrEmpty(userID)) {
                // If the session creator left, I need to leave also.
                if (_session.sessionAdministrator == userID) {
                    Debug.Log("OrchestratorController: OnUserLeftSession: Session creator " + _session.GetUser(userID).userName + " left the session. Also leaving.");
                    LeaveSession();
                }
                // Otherwise, just proceed to the common user left event.
                else {
                    if (enableLogging) Debug.Log("OrchestratorController: OnUserLeftSession: User " + _session.GetUser(userID).userName + " left the session. Getting new session info.");

                    // Required to update the list of connect users.
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

        #endregion

        #region Messages

        public void SendMessage(string pMessage, string pUserID) {
            _orchestratorWrapper.SendMessage(pMessage, pUserID);
        }

        void IOrchestratorResponsesListener.OnSendMessageResponse(ResponseStatus status) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
            }
        }

        public void SendMessageToAll(string pMessage) {
            _orchestratorWrapper.SendMessageToAll(pMessage);
        }

        void IOrchestratorResponsesListener.OnSendMessageToAllResponse(ResponseStatus status) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
            }
        }

        // Message from a user received spontaneously from the Orchestrator
        void IUserMessagesListener.OnUserMessageReceived(UserMessage userMessage) {
            OnUserMessageReceivedEvent?.Invoke(userMessage);
        }

        #endregion

        #region Events

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

            // Check frequently if there is users connected and ensure a null session (from the delete command) is escaped.
            while (_session.sessionUsers.Length > 0) {
                GetSessionInfo();
                yield return new WaitForSeconds(1.0f);
            }

            // When the session is free of users, delete it.
            if (_session.sessionUsers.Length == 0) {
                DeleteSession(_session.sessionId);
            }
        }

        #endregion

        #region Errors

        void IOrchestratorResponsesListener.OnError(ResponseStatus status) {
            if (enableLogging) Debug.Log("OrchestratorController: OnError: Error code: " + status.Error + "::Error message: " + status.Message);

            OnErrorEvent?.Invoke(status);
        }

        #endregion
    }
}
