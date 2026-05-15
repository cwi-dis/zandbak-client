#pragma warning disable CS0618

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Orchestrator.Data;
using Orchestrator.Util;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.PackageManager;
using UnityEditor.Search;
#endif

namespace Orchestrator.Wrapping
{
    public class OrchestratorController : MonoBehaviour, IOrchestratorResponsesListener, IUserMessagesListener, IUserSessionEventsListener, IOrchestratorEventsListener, IBubbleEventsListener
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
        public OrchestratorWrapper Wrapper => _orchestratorWrapper;
        public Uri SocketUrl { get; private set; }

        //Session
        private Session _session;

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
        /// Invoked when the current session is being closed remotely
        /// </summary>
        public event Action OnSessionCloseEvent;

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
        /// Invoked when a new user joins the current session, with the user ID as argument
        /// </summary>
        public event Action<string, User> OnUserJoinSessionEvent;

        /// <summary>
        /// Invoked when a user leaves the current session, with the user ID as argument
        /// </summary>
        public event Action<string, bool> OnUserLeaveSessionEvent;

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
        /// Invoked when a new session is created
        /// </summary>
        public event Action<Session> OnSessionCreatedEvent;

        /// <summary>
        /// Invoked when a new session is deleted
        /// </summary>
        public event Action<Session> OnSessionDeletedEvent;

        /// <summary>
        /// Invoked when a broadcast is received in the current session
        /// </summary>
        public event Action<BroadcastData> OnBroadcastReceivedEvent;

        /// <summary>
        /// Invoked when the current user is invited to a bubble
        /// </summary>
        public event Action<string> OnBubbleInvited;

        /// <summary>
        /// Invoked when a user joins the current user's bubble
        /// </summary>
        public event Action<User> OnBubbleJoined;

        /// <summary>
        /// Invoked when a user leaves the current user's bubble
        /// </summary>
        public event Action<User> OnBubbleLeft;

        /// <summary>
        /// Invoked when a user requests to join the current user's bubble
        /// </summary>
        public event Action<User> OnBubbleJoinRequested;

        /// <summary>
        /// Invoked when a bubble join request is approved or denied by the owner of the bubble
        /// </summary>
        public event Action<string, bool> OnBubbleJoinRequestApproved;

        #endregion

        #region public properties

        public bool ConnectedToOrchestrator => _connectedToOrchestrator;
        public OrchestratorConnectionStatus ConnectionStatus => _connectionStatus;

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

            SocketUrl = new Uri(url);
            _orchestratorWrapper = new OrchestratorWrapper(url, this, this, this, this, this);
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

        void IOrchestratorResponsesListener.OnDisconnect() {
            Debug.LogWarning($"OrchestratorController: disconnected from orchestrator");
            _connectedToOrchestrator = false;
            _connectionStatus = OrchestratorConnectionStatus.Disconnected;
            SocketUrl = null;
            OnConnectionEvent?.Invoke(false);
        }

        #endregion

        public static string DeviceTypeToString(DeviceType deviceType) => deviceType switch
        {
            DeviceType.VR => "vr",
            DeviceType.AR => "ar",
            _ => "unknown"
        };

        #region Sessions

        void IUserSessionEventsListener.OnSessionClosed()
        {
            // The session has been closed by the session creator.
            Log("OrchestratorController: OnSessionClosed: Current session closed by session creator.");

            OnSessionCloseEvent?.Invoke();
        }

        void IUserSessionEventsListener.OnUserJoinedSession(string userID, User user) {
            OnUserJoinSessionEvent?.Invoke(userID, user);
        }

        void IUserSessionEventsListener.OnUserLeftSession(string userID, bool force) {
            if (!string.IsNullOrEmpty(userID)) {
                OnUserLeaveSessionEvent?.Invoke(userID, force);
            }
        }

        void IUserSessionEventsListener.OnUserRaisedHand(string userID)
        {
            OnUserRaisedHandEvent?.Invoke(userID);
        }

        void IUserSessionEventsListener.OnUserClearedRaisedHand(string userID)
        {
            OnUserClearedRaisedHandEvent?.Invoke(userID);
        }

        void IUserSessionEventsListener.OnSessionStatusChanged(string status)
        {
            OnSessionStatusChangedEvent?.Invoke(status);
        }

        void IUserSessionEventsListener.OnPresentationChanged(Presentation presentation)
        {
            OnSessionPresentationChangedEvent?.Invoke(presentation);
        }

        void IUserSessionEventsListener.OnPresentationIsSharingChanged(Presentation presentation)
        {
            OnSessionPresentationIsSharingEvent?.Invoke(presentation);
        }

        void IUserSessionEventsListener.OnSlideChanged(Presentation presentation)
        {
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

        void IOrchestratorEventsListener.OnSessionCreated(Session session)
        {
            OnSessionCreatedEvent?.Invoke(session);
        }

        void IOrchestratorEventsListener.OnSessionDeleted(Session session)
        {
            OnSessionDeletedEvent?.Invoke(session);
        }

        void IUserSessionEventsListener.OnBubbleInvited(string bubbleId)
        {
            OnBubbleInvited?.Invoke(bubbleId);
        }


        void IUserSessionEventsListener.OnBubbleJoinRequestApproved(string bubbleId, bool approved)
        {
            OnBubbleJoinRequestApproved?.Invoke(bubbleId, approved);
        }

        void IBubbleEventsListener.OnBubbleJoinRequested(User user)
        {
            OnBubbleJoinRequested?.Invoke(user);
        }

        void IBubbleEventsListener.OnBubbleJoined(User user)
        {
            OnBubbleJoined?.Invoke(user);
        }

        void IBubbleEventsListener.OnBubbleLeft(User user)
        {
            OnBubbleLeft?.Invoke(user);
        }

        #endregion

        #region Messages

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
                // Get the version of the Orchestrator
                Wrapper.GetOrchestratorVersion((_, version) =>
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
                });
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
