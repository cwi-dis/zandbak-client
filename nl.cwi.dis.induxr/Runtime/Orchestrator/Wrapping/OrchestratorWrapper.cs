using System;
using System.Collections.Generic;
using System.Text;
using Orchestrator.Data;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using SocketIOClient.Transport;
using UnityEngine;

namespace Orchestrator.Wrapping {
    public class OrchestratorWrapper : IOrchestratorConnectionListener
    {
        private SocketIOUnity _socket;
        private readonly object _sendLock = new();

        private static OrchestratorWrapper _instance;
        // Listener for the responses of the orchestrator
        private readonly IOrchestratorResponsesListener _responsesListener;

        // Listener for the messages emitted spontaneously by the orchestrator
        private readonly IUserMessagesListener _userMessagesListener;

        // Listener for the user events emitted when a session is updated by the orchestrator
        private readonly IUserSessionEventsListener _userSessionEventListener;

        // Listener for the events emitted when an event on a conversation bubble occurs
        private readonly IBubbleEventsListener _bubbleEventListener;

        // Listener for the events emitted when an Orchestrator-wide event occurs
        private readonly IOrchestratorEventsListener _orchestratorEventListener;

        public Action<UserDataStreamPacket> OnDataStreamReceived;

        public OrchestratorWrapper(string orchestratorSocketUrl, IOrchestratorResponsesListener responsesListener, IUserMessagesListener userMessagesListener, IUserSessionEventsListener userSessionEventsListener, IOrchestratorEventsListener orchestratorEventListener, IBubbleEventsListener bubbleEventListener)
        {
            _instance ??= this;

            _responsesListener = responsesListener;
            _userMessagesListener = userMessagesListener;
            _userSessionEventListener = userSessionEventsListener;
            _orchestratorEventListener = orchestratorEventListener;
            _bubbleEventListener = bubbleEventListener;

            _socket = new SocketIOUnity(new Uri(orchestratorSocketUrl), new SocketIOOptions {
                Transport = TransportProtocol.WebSocket,
                Reconnection = false,
                EIO = EngineIO.V4
            });
            _socket.JsonSerializer = new NewtonsoftJsonSerializer();

            _socket.OnConnected += (_, _) => OnSocketConnect();
            _socket.OnDisconnected += (_, _) =>
            {
                OnSocketDisconnect();
            };
            _socket.OnError += (_, e) =>
            {
                Debug.LogError($"ERROR: {e}");
                OnSocketError(null);
            };

            _socket.On("Broadcast", OnBroadcastReceived);
            _socket.On("MessageSent", OnMessageSentFromOrchestrator);
            _socket.On("DataReceived", OnUserDataReceived);
            _socket.On("SessionUpdated", OnSessionUpdated);
            _socket.On("SessionClosed", OnSessionClosed);
            _socket.On("OrchestratorUpdated", OnOrchestratorUpdated);
            _socket.On("BubbleUpdated", OnBubbleUpdated);
        }

        public void Connect()
        {
            lock (_sendLock)
            {
                _socket.Connect();
            }

            OnSocketConnecting();
        }

        public void OnSocketConnect()
        {
            if (_responsesListener == null)
            {
                Debug.LogWarning($"OrchestratorWrapper: OnSocketConnect: no ResponsesListener");
            }
            else
            {
                Debug.Log("Calling OnConnect");
                UnityThread.executeInUpdate(() => {
                  _responsesListener.OnConnect();
                });
            }
        }

        public void Disconnect()
        {
            lock (_sendLock)
            {
                Debug.Log("DISCONNECT called");
                _socket.Disconnect();
            }
        }

        public void OnSocketDisconnect()
        {
            if (_responsesListener == null)
            {
                Debug.LogWarning($"OrchestratorWrapper: OnSocketDisconnect: no ResponsesListener");
            }
            else
            {
                UnityThread.executeInUpdate(() => {
                    _responsesListener.OnDisconnect();
                });
            }
        }

        public void OnSocketConnecting()
        {
            UnityThread.executeInUpdate(() =>
            {
                _responsesListener?.OnConnecting();
            });
        }

        public void OnSocketError(ResponseStatus message)
        {
            throw new NotImplementedException();
        }

        #region utility requests

        public void GetOrchestratorVersion(Action<ResponseStatus, string> callback) {
            lock (this) {
                _socket.Emit("GetOrchestratorVersion", (response) => {
                    var data = response.GetValue<OrchestratorResponse<VersionResponse>>();

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus, data.Body.OrchestratorVersion);
                    });
                }, new { });
            }
        }

        public void GetNtpTime(Action<ResponseStatus, NtpClock> callback) {
            lock (this) {
                _socket.Emit("GetNTPTime", (response) => {
                    var data = response.GetValue<OrchestratorResponse<NtpClock>>();

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus, data.Body);
                    });
                }, new { });
            }
        }

        #endregion

        #region login/logout

        public void Login(string userName, string deviceType, Action<ResponseStatus, User> callback) {
            lock (this) {
                _socket.Emit("Login", (response) => {
                    var data = response.GetValue<OrchestratorResponse<LoginResponse>>();

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus, data.Body.UserData);
                    });
                }, new {
                    userName, deviceType
                });
            }
        }

        public void Login(string userName, string password, string deviceType, Action<ResponseStatus, User> callback) {
            lock (this) {
                _socket.Emit("Login", (response) => {
                    var data = response.GetValue<OrchestratorResponse<LoginResponse>>();

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus, data.Body.UserData);
                    });
                }, new {
                    userName, password, deviceType
                });
            }
        }

        public void Logout(Action<bool> callback) {
            lock (this) {
                _socket.Emit("Logout", (response) => {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus.Error == 0);
                    });
                }, new { });
            }
        }

        #endregion

        #region session management

        public void AddSession(string sessionName, string sessionDescription, string sessionProtocol, string sessionRoom, string[] channels, bool persistent, Action<ResponseStatus, Session> callback) {
            lock (this) {
                _socket.Emit("AddSession", (response) => {
                    var data = response.GetValue<OrchestratorResponse<Session>>();

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus, data.Body);
                    });
                }, new {
                    sessionName,
                    sessionDescription,
                    sessionProtocol,
                    sessionRoom,
                    channels,
                    persistent
                });
            }
        }

        public void ScheduleSession(string sessionId, Action<ResponseStatus, Session> callback) {
            lock (this) {
                _socket.Emit("ScheduleSession", (response) => {
                    var data = response.GetValue<OrchestratorResponse<Session>>();

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus, data.Body);
                    });
                }, new {
                    sessionId
                });
            }
        }

        public void GetSessions(Action<ResponseStatus, List<Session>> callback) {
            lock (this) {
                _socket.Emit("GetSessions", (response) => {
                    var data = response.GetValue<OrchestratorResponse<Dictionary<string, Session>>>();

                    var sessions = new List<Session>();
                    foreach (var item in data.Body) {
                        sessions.Add(item.Value);
                    }

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus, sessions);
                    });
                }, new { });
            }
        }

        public void GetScheduledSessions(Action<ResponseStatus, List<ScheduledSession>> callback) {
            lock (this) {
                _socket.Emit("GetScheduledSessions", (response) => {
                    var data = response.GetValue<OrchestratorResponse<List<ScheduledSession>>>();

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus, data.Body);
                    });
                }, new { });
            }
        }

        public void GetSessionInfo(Action<ResponseStatus, Session> callback) {
            lock (this) {
                _socket.Emit("GetSessionInfo", (response) => {
                    var data = response.GetValue<OrchestratorResponse<Session>>();

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus, data.Body);
                    });
                }, new { });
            }
        }

        public void DeleteSession(string sessionId, Action<ResponseStatus> callback) {
            lock (this) {
                _socket.Emit("DeleteSession", (response) => {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus);
                    });
                }, new {
                    sessionId
                });
            }
        }

        public void JoinSession(string sessionId, Action<ResponseStatus, Session> callback) {
            lock (this) {
                _socket.Emit("JoinSession", (response) => {
                    var data = response.GetValue<OrchestratorResponse<Session>>();

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus, data.Body);
                    });
                }, new {
                    sessionId
                });
            }
        }

        public void LeaveSession(Action<ResponseStatus> callback) {
            lock (this) {
                _socket.Emit("LeaveSession", (response) => {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus);
                    });
                }, new {});
            }
        }

        public void LeaveSession(string userId, Action<ResponseStatus> callback)
        {
            lock (this) {
                _socket.Emit("LeaveSession", (response) => {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus);
                    });
                }, new { userId });
            }
        }

        public void IsSpeaking(bool isSpeaking, Action<bool> callback)
        {
            lock (this) {
                _socket.Emit("IsSpeaking", (response) => {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus.Error == 0);
                    });
                }, new {
                    isSpeaking
                });
            }
        }

        public void SendMessage(string message, string userId, Action<ResponseStatus> callback) {
            lock (this) {
                _socket.Emit("SendMessage", (response) => {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus);
                    });
                }, new {
                    message,
                    userId
                });
            }
        }

        public void SendMessageToAll(string message, Action<ResponseStatus> callback) {
            lock (this) {
                _socket.Emit("SendMessageToAll", (response) => {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus);
                    });
                }, new {
                    message
                });
            }
        }

        public void GetMessages(Action<ResponseStatus, List<ChatMessage>> callback)
        {
            lock (this) {
                _socket.Emit("GetMessages", (response) => {
                    var data = response.GetValue<OrchestratorResponse<List<ChatMessage>>>();

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus, data.Body);
                    });
                }, new {});
            }
        }

        public void GetMessages(int count, Action<ResponseStatus, List<ChatMessage>> callback)
        {
            lock (this) {
                _socket.Emit("GetMessages", (response) => {
                    var data = response.GetValue<OrchestratorResponse<List<ChatMessage>>>();

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus, data.Body);
                    });
                }, new {
                    count
                });
            }
        }

        public void RaiseHand(Action<ResponseStatus> callback)
        {
            lock (this)
            {
                _socket.Emit("RaiseHand", (response) =>
                {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus);
                    });
                }, new {});
            }
        }

        public void ClearRaisedHand(string userId, Action<ResponseStatus> callback)
        {
            lock (this)
            {
                _socket.Emit("ClearRaisedHand", (response) =>
                {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus);
                    });
                }, new { userId });
            }
        }

        public void ClearRaisedHand(Action<ResponseStatus> callback)
        {
            lock (this)
            {
                _socket.Emit("ClearRaisedHand", (response) =>
                {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus);
                    });
                }, new {});
            }
        }

        public void GetRaisedHands(Action<ResponseStatus, List<User>> callback)
        {
            lock (this)
            {
                _socket.Emit("GetRaisedHands", (response) =>
                {
                    var data = response.GetValue<OrchestratorResponse<List<User>>>();

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus, data.Body);
                    });
                }, new {});
            }
        }

        public void GoToNextPresentation(Action<ResponseStatus, Presentation> callback)
        {
            lock (this)
            {
                _socket.Emit("SetSessionPresentation", (response) =>
                {
                    var data = response.GetValue<OrchestratorResponse<PresentationResponse>>();

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus, data.Body.Presentation);
                    });
                }, new {});
            }
        }

        public void GoToPresentation(int presentationIndex, Action<ResponseStatus, Presentation> callback)
        {
            lock (this)
            {
                _socket.Emit("SetSessionPresentation", (response) =>
                {
                    var data = response.GetValue<OrchestratorResponse<PresentationResponse>>();

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus, data.Body.Presentation);
                    });
                }, new { presentationIndex });
            }
        }

        public void ChangeSlide(int slideOffset, Action<ResponseStatus, Presentation> callback)
        {
            lock (this)
            {
                _socket.Emit("ChangeSlide", (response) =>
                {
                    var data = response.GetValue<OrchestratorResponse<PresentationResponse>>();

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus, data.Body.Presentation);
                    });
                }, new { slideOffset });
            }
        }

        public void SetSlide(int slideIndex, Action<ResponseStatus, Presentation> callback)
        {
            lock (this)
            {
                _socket.Emit("ChangeSlide", (response) =>
                {
                    var data = response.GetValue<OrchestratorResponse<PresentationResponse>>();

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus, data.Body.Presentation);
                    });
                }, new { slideIndex });
            }
        }

        public void CurrentPresentationIsSharing(bool isSharing, Action<ResponseStatus, Presentation> callback)
        {
            lock (this)
            {
                _socket.Emit("IsSharing", (response) =>
                {
                    var data = response.GetValue<OrchestratorResponse<PresentationResponse>>();

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus, data.Body.Presentation);
                    });
                }, new { isSharing });
            }
        }

        public void SetSessionStatus(string status, Action<ResponseStatus, string> callback)
        {
            lock (this)
            {
                _socket.Emit("ChangeSlide", (response) =>
                {
                    var data = response.GetValue<OrchestratorResponse<StatusResponse>>();

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus, data.Body.Status);
                    });
                }, new { status });
            }
        }

        public void SetUserStatus(string status, Action<ResponseStatus, string> callback)
        {
            lock (this)
            {
                _socket.Emit("SetUserStatus", (response) =>
                {
                    var data = response.GetValue<OrchestratorResponse<StatusResponse>>();

                    UnityThread.executeInUpdate(() => {
                        callback(data.ResponseStatus, data.Body.Status);
                    });
                }, new { status });
            }
        }

        public void GetRooms(Action<ResponseStatus, List<Room>> callback)
        {
            lock (this)
            {
                _socket.Emit("GetRooms", (response) =>
                {
                    var data = response.GetValue<OrchestratorResponse<List<Room>>>();
                    UnityThread.executeInUpdate(() =>
                    {
                        callback(data.ResponseStatus, data.Body);
                    });
                }, new {});
            }
        }

        public void CreateBubble(string name, Action<ResponseStatus, Bubble> callback)
        {
            lock (this)
            {
                _socket.Emit("CreateBubble", (response) =>
                {
                    var data = response.GetValue<OrchestratorResponse<Bubble>>();
                    UnityThread.executeInUpdate(() =>
                    {
                        callback(data.ResponseStatus, data.Body);
                    });
                }, new { name });
            }
        }

        public void ListBubbles(Action<ResponseStatus, List<Bubble>> callback)
        {
            lock (this)
            {
                _socket.Emit("ListBubbles", (response) =>
                {
                    var data = response.GetValue<OrchestratorResponse<List<Bubble>>>();
                    UnityThread.executeInUpdate(() =>
                    {
                        callback(data.ResponseStatus, data.Body);
                    });
                }, new { });
            }
        }

        public void GetBubble(string bubbleId, Action<ResponseStatus, Bubble> callback)
        {
            lock (this)
            {
                _socket.Emit("GetBubble", (response) =>
                {
                    var data = response.GetValue<OrchestratorResponse<Bubble>>();
                    UnityThread.executeInUpdate(() =>
                    {
                        callback(data.ResponseStatus, data.Body);
                    });
                }, new { bubbleId });
            }
        }

        public void LeaveBubble(Action<ResponseStatus, Bubble> callback)
        {
            lock (this)
            {
                _socket.Emit("LeaveBubble", (response) =>
                {
                    var data = response.GetValue<OrchestratorResponse<Bubble>>();
                    UnityThread.executeInUpdate(() =>
                    {
                        callback(data.ResponseStatus, data.Body);
                    });
                }, new {});
            }
        }

        public void InviteToBubble(string userId, Action<ResponseStatus> callback)
        {
            lock (this)
            {
                _socket.Emit("SendBubbleInvitation", (response) =>
                {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();
                    UnityThread.executeInUpdate(() =>
                    {
                        callback(data.ResponseStatus);
                    });
                }, new { userId });
            }
        }

        public void JoinBubble(string bubbleId, Action<ResponseStatus> callback)
        {
            lock (this)
            {
                _socket.Emit("JoinBubble", (response) =>
                {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();
                    UnityThread.executeInUpdate(() =>
                    {
                        callback(data.ResponseStatus);
                    });
                }, new { bubbleId });
            }
        }

        public void RequestBubbleJoin(string bubbleId, Action<ResponseStatus> callback)
        {
            lock (this)
            {
                _socket.Emit("RequestJoinBubble", (response) =>
                {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();
                    UnityThread.executeInUpdate(() =>
                    {
                        callback(data.ResponseStatus);
                    });
                }, new { bubbleId });
            }
        }

        public void ApproveBubbleJoinRequest(string userId, string bubbleId, bool approve, Action<ResponseStatus> callback)
        {
            lock (this)
            {
                _socket.Emit("ApproveJoinBubble", (response) =>
                {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();
                    UnityThread.executeInUpdate(() =>
                    {
                        callback(data.ResponseStatus);
                    });
                }, new { userId, bubbleId, approve });
            }
        }

        #endregion

        #region shared objects

        public void RegisterSharedObject(string id, Transform initialTransform, Action<ResponseStatus, SharedObject> callback)
        {
            var position = new { initialTransform.position.x, initialTransform.position.y, initialTransform.position.z };
            var rotation = new { initialTransform.rotation.x, initialTransform.rotation.y, initialTransform.rotation.z, initialTransform.rotation.w };

            lock (this)
            {
                _socket.Emit("RegisterSharedObject", (response) =>
                {
                    var data = response.GetValue<OrchestratorResponse<SharedObject>>();
                    UnityThread.executeInUpdate(() =>
                    {
                        callback(data.ResponseStatus, data.Body);
                    });
                }, new { id, position, rotation });
            }
        }

        public void ClaimObjectOwnership(string objectId, Action<ResponseStatus, SharedObject> callback)
        {
            lock (this)
            {
                _socket.Emit("ClaimOwnership", (response) =>
                {
                    var data = response.GetValue<OrchestratorResponse<SharedObject>>();
                    UnityThread.executeInUpdate(() =>
                    {
                        callback(data.ResponseStatus, data.Body);
                    });
                }, new { objectId, type = "object" });
            }
        }

        #endregion

        #region broadcasts

        public void SendBroadcastToChannel(string channel, byte[] pByteArray) {
            lock (_sendLock) {
                _socket.Emit("Broadcast",
                    channel,
                    pByteArray
                );
            }
        }

        #endregion

        #region events

        private void OnMessageSentFromOrchestrator(SocketIOResponse response) {
            lock (this) {
                var message = response.GetValue<ChatMessage>();
                UnityThread.executeInUpdate(() => {
                    _userMessagesListener?.OnUserMessageReceived(message);
                });
            }
        }

        private void OnUserDataReceived(SocketIOResponse response) {
            lock (this) {
                var userId = response.GetValue<string>();
                var type = response.GetValue<string>(1);
                var data = response.GetValue<byte[]>(2);

                var packet = new UserDataStreamPacket(userId, type, "", data);

                UnityThread.executeInUpdate(() =>
                {
                    OnDataStreamReceived?.Invoke(packet);
                });
            }
        }

        private void OnBroadcastReceived(SocketIOResponse response) {
            lock (this) {
                if (_userMessagesListener != null)
                {
                        var channel = response.GetValue<string>();
                        string data = Encoding.ASCII.GetString(response.InComingBytes[0], 0, response.InComingBytes[0].Length);

                        UnityThread.executeInUpdate(() =>
                        {
                            _userMessagesListener.OnBroadcastReceived(new BroadcastData(channel, data));
                        });
                }
                else
                {
                    Debug.LogWarning("No UserMessagesListener");
                }
            }
        }

        private void OnBubbleUpdated(SocketIOResponse response)
        {
            lock (this)
            {
                var data = response.GetValue<BubbleUpdate<User>>();

                switch (data.EventId)
                {
                    case "USER_JOINED_BUBBLE":
                        UnityThread.executeInUpdate(() =>
                        {
                            Debug.Log("User joined bubble");
                            _bubbleEventListener.OnBubbleJoined(data.EventData);
                        });
                        break;
                    case "USER_LEFT_BUBBLE":
                        UnityThread.executeInUpdate(() =>
                        {
                            Debug.Log("User left bubble");
                            _bubbleEventListener.OnBubbleLeft(data.EventData);
                        });
                        break;
                }
            }
        }

        private void OnOrchestratorUpdated(SocketIOResponse response)
        {
            lock (this)
            {
                var data = response.GetValue<OrchestratorUpdate<EmptyUpdate>>();

                switch (data.EventId)
                {
                    case "SESSION_CREATED":
                        UnityThread.executeInUpdate(() =>
                        {
                            var sessionData = response.GetValue<OrchestratorUpdate<Session>>();
                            _orchestratorEventListener?.OnSessionCreated(sessionData.EventData);
                        });
                        break;
                    case "SESSION_DELETED":
                        UnityThread.executeInUpdate(() =>
                        {
                            var sessionData = response.GetValue<OrchestratorUpdate<Session>>();
                            _orchestratorEventListener?.OnSessionDeleted(sessionData.EventData);
                        });
                        break;
                }
            }
        }

        private void OnSessionUpdated(SocketIOResponse response) {
            lock (this) {
                var data = response.GetValue<SessionUpdate<EmptyUpdate>>();

                switch (data.EventId) {
                    case "USER_JOINED_SESSION":
                    case "USER_RAISED_HAND":
                    case "USER_CLEARED_RAISED_HAND":
                        OnSessionUpdatedWithUserData(response);
                        break;
                    case "USER_LEFT_SESSION":
                        OnSessionUpdatedWithIsForcedData(response);
                        break;
                    case "USER_STATUS_UPDATED":
                        OnSessionUpdatedWithUserStatus(response);
                        break;
                    case "PRESENTATION_CHANGED":
                    case "SLIDE_CHANGED":
                    case "PRESENTATION_IS_SHARING":
                        OnSessionUpdatedWithPresentationData(response);
                        break;
                    case "SESSION_STATUS_CHANGED":
                        OnSessionUpdatedWithStatusData(response);
                        break;
                    case "USER_IS_SPEAKING":
                        OnSessionUpdatedWithIsSpeakingData(response);
                        break;
                    case "BUBBLE_JOIN_REQUESTED":
                        Debug.Log("Bubble join requested");
                        OnSessionUpdatedWithUser(response);
                        break;
                    case "BUBBLE_JOIN_REQUEST_APPROVED":
                    case "BUBBLE_JOIN_INVITED":
                        OnSessionUpdatedWithBubbleId(response);
                        break;
                    case "OBJECT_REGISTERED":
                    case "OBJECT_OWNERSHIP_CHANGED":
                        OnSessionUpdatedWithObjectUpdate(response);
                        break;
                }
            }
        }

        private void OnSessionClosed(SocketIOResponse response)
        {
            UnityThread.executeInUpdate(() =>
            {
                _userSessionEventListener?.OnSessionClosed();
            });
        }

        private void OnSessionUpdatedWithObjectUpdate(SocketIOResponse response)
        {
            var data = response.GetValue<SessionUpdate<SharedObject>>();

            UnityThread.executeInUpdate(() =>
            {
                switch (data.EventId)
                {
                    case "OBJECT_REGISTERED":
                        _userSessionEventListener.OnObjectRegistered(data.EventData);
                        break;
                    case "OBJECT_OWNERSHIP_CHANGED":
                        _userSessionEventListener.OnObjectOwnershipChanged(data.EventData);
                        break;
                }
            });
        }

        private void OnSessionUpdatedWithBubbleId(SocketIOResponse response)
        {
            var data = response.GetValue<SessionUpdate<SessionUpdateBubbleId>>();

            UnityThread.executeInUpdate(() =>
            {
                if (data.EventData.Approved != null)
                {
                    _userSessionEventListener?.OnBubbleJoinRequestApproved(data.EventData.BubbleId, (bool)data.EventData.Approved);
                }
                else
                {
                    _userSessionEventListener?.OnBubbleInvited(data.EventData.BubbleId);
                }
            });
        }

        private void OnSessionUpdatedWithUser(SocketIOResponse response)
        {
            var data = response.GetValue<SessionUpdate<User>>();

            UnityThread.executeInUpdate(() =>
            {
                _bubbleEventListener?.OnBubbleJoinRequested(data.EventData);
            });
        }

        private void OnSessionUpdatedWithUserStatus(SocketIOResponse response)
        {
            var data = response.GetValue<SessionUpdate<SessionUpdateUserStatus>>();

            UnityThread.executeInUpdate(() =>
            {
                _userSessionEventListener?.OnUserStatusChanged(data.EventData.UserId, data.EventData.Status);
            });
        }

        private void OnSessionUpdatedWithIsForcedData(SocketIOResponse response)
        {
            var data = response.GetValue<SessionUpdate<SessionUpdateIsForceData>>();
            var eventData = data.EventData;

            UnityThread.executeInUpdate(() =>
            {
                _userSessionEventListener?.OnUserLeftSession(eventData.UserId, eventData.Force);
            });
        }

        private void OnSessionUpdatedWithIsSpeakingData(SocketIOResponse response)
        {
            var data = response.GetValue<SessionUpdate<SessionUpdateIsSpeakingData>>();

            UnityThread.executeInUpdate(() =>
            {
                _userSessionEventListener?.OnSessionIsSpeakingChanged(data.EventData.UserId, data.EventData.IsSpeaking);
            });
        }

        private void OnSessionUpdatedWithStatusData(SocketIOResponse response)
        {
            var data = response.GetValue<SessionUpdate<SessionUpdateStatusData>>();

            UnityThread.executeInUpdate(() =>
            {
                _userSessionEventListener?.OnSessionStatusChanged(data.EventData.Status);
            });
        }

        private void OnSessionUpdatedWithPresentationData(SocketIOResponse response)
        {
            var data = response.GetValue<SessionUpdate<SessionUpdatePresentationData>>();

            switch (data.EventId)
            {
                case "PRESENTATION_CHANGED":
                    UnityThread.executeInUpdate(() =>
                    {
                        _userSessionEventListener?.OnPresentationChanged(data.EventData.Presentation);
                    });
                    break;
                case "PRESENTATION_IS_SHARING":
                    UnityThread.executeInUpdate(() =>
                    {
                        _userSessionEventListener?.OnPresentationIsSharingChanged(data.EventData.Presentation);
                    });
                    break;
                case "SLIDE_CHANGED":
                    UnityThread.executeInUpdate(() =>
                    {
                        _userSessionEventListener?.OnSlideChanged(data.EventData.Presentation);
                    });
                    break;
            }
        }

        private void OnSessionUpdatedWithUserData(SocketIOResponse response)
        {
            var data = response.GetValue<SessionUpdate<SessionUpdateUserData>>();
            var eventData = data.EventData;

            switch (data.EventId)
            {
                case "USER_JOINED_SESSION":
                    UnityThread.executeInUpdate(() =>
                    {
                        _userSessionEventListener?.OnUserJoinedSession(eventData.UserId, eventData.UserData);
                    });
                    break;
                case "USER_RAISED_HAND":
                    UnityThread.executeInUpdate(() =>
                    {
                        _userSessionEventListener?.OnUserRaisedHand(eventData.UserId);
                    });
                    break;
                case "USER_CLEARED_RAISED_HAND":
                    UnityThread.executeInUpdate(() =>
                    {
                        _userSessionEventListener?.OnUserClearedRaisedHand(eventData.UserId);
                    });
                    break;
            }
        }

        #endregion
    }
}
