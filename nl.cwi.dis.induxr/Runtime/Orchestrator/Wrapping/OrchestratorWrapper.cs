using System;
using System.Collections.Generic;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using UnityEngine;
using System.Text;

using Orchestrator.Responses;
using Orchestrator.Interfaces;
using Orchestrator.Elements;

namespace Orchestrator.Wrapping {
    public class OrchestratorWrapper : IOrchestratorConnectionListener
    {
        private SocketIOUnity _socket;
        private readonly object _sendLock = new();

        private static OrchestratorWrapper _instance;
        // Listener for the responses of the orchestrator
        private IOrchestratorResponsesListener _responsesListener;

        // Listener for the messages emitted spontaneously by the orchestrator
        private IUserMessagesListener _userMessagesListener;

        // Listeners for the user events emitted when a session is updated by the orchestrator
        private List<IUserSessionEventsListener> _userSessionEventslisteners;

        public Action<UserDataStreamPacket> OnDataStreamReceived;
        private string _myUserID = "";

        public OrchestratorWrapper(string orchestratorSocketUrl, IOrchestratorResponsesListener responsesListener, IUserMessagesListener userMessagesListener, IUserSessionEventsListener userSessionEventsListener)
        {
            if (_instance is null)
            {
                _instance = this;
            }

            _responsesListener = responsesListener;
            _userMessagesListener = userMessagesListener;

            _userSessionEventslisteners = new List<IUserSessionEventsListener> {
                userSessionEventsListener
            };

            _socket = new SocketIOUnity(new Uri(orchestratorSocketUrl), new SocketIOOptions {
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
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

            _socket.OnPing += (_, _) => {
                Debug.Log("PING");
            };

            _socket.OnPong += (_, _) => {
                Debug.Log("PoNG");
            };

            _socket.On("Broadcast", OnBroadcastReceived);
            _socket.On("MessageSent", OnMessageSentFromOrchestrator);
            _socket.On("DataReceived", OnUserDataReceived);
            _socket.On("SceneEventToMaster", OnMasterEventReceived);
            _socket.On("SceneEventToUser", OnUserEventReceived);
            _socket.On("SessionUpdated", OnSessionUpdated);
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

        public void GetOrchestratorVersion() {
            lock (this) {
                _socket.Emit("GetOrchestratorVersion", (response) => {
                    var data = response.GetValue<OrchestratorResponse<VersionResponse>>();
                    
                    UnityThread.executeInUpdate(() => {
                        _responsesListener?.OnGetOrchestratorVersionResponse(data.ResponseStatus, data.body.orchestratorVersion);
                    });
                }, new { });
            }
        }

        public void GetNtpTime() {
            lock (this) {
                _socket.Emit("GetNTPTime", (response) => {
                    var data = response.GetValue<OrchestratorResponse<NtpClock>>();

                    UnityThread.executeInUpdate(() => {
                        _responsesListener?.OnGetNTPTimeResponse(data.ResponseStatus, data.body);
                    });
                }, new { });
            }
        }

        #endregion

        #region login/logout

        public void Login(string username) {
            lock (this) {
                _socket.Emit("Login", (response) => {
                    var data = response.GetValue<OrchestratorResponse<LoginResponse>>();
                    _myUserID = data.body.userId;

                    UnityThread.executeInUpdate(() => {
                        _responsesListener?.OnLoginResponse(data.ResponseStatus, data.body.userId);
                    });
                }, new {
                    userName = username
                });
            }
        }
        
        public void Login(string username, string password) {
            lock (this) {
                _socket.Emit("Login", (response) => {
                    var data = response.GetValue<OrchestratorResponse<LoginResponse>>();
                    _myUserID = data.body.userId;

                    UnityThread.executeInUpdate(() => {
                        _responsesListener?.OnLoginResponse(data.ResponseStatus, data.body.userId);
                    });
                }, new {
                    userName = username,
                    password
                });
            }
        }

        public void Logout() {
            lock (this) {
                _socket.Emit("Logout", (response) => {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();
                    _myUserID = "";

                    UnityThread.executeInUpdate(() => {
                        _responsesListener?.OnLogoutResponse(data.ResponseStatus);
                    });
                }, new { });
            }
        }

        #endregion

        #region session management

        public void AddSession(string scenarioId, Scenario scenario, string sessionName, string sessionDescription, string sessionProtocol, string[] channels) {
            lock (this) {
                _socket.Emit("AddSession", (response) => {
                    var data = response.GetValue<OrchestratorResponse<Session>>();

                    UnityThread.executeInUpdate(() => {
                        _responsesListener?.OnAddSessionResponse(data.ResponseStatus, data.body);
                    });
                }, new {
                    sessionName,
                    sessionDescription,
                    sessionProtocol,
                    scenarioDefinition = new {
                        scenarioId,
                        scenario.scenarioName,
                        scenario.scenarioDescription
                    },
                    channels
                });
            }
        }

        public void GetSessions() {
            lock (this) {
                _socket.Emit("GetSessions", (response) => {
                    var data = response.GetValue<OrchestratorResponse<Dictionary<string, Session>>>();

                    var sessions = new List<Session>();
                    foreach (var item in data.body) {
                        sessions.Add(item.Value);
                    }

                    UnityThread.executeInUpdate(() => {
                        _responsesListener?.OnGetSessionsResponse(data.ResponseStatus, sessions);
                    });
                }, new { });
            }
        }

        public void GetSessionInfo() {
            lock (this) {
                _socket.Emit("GetSessionInfo", (response) => {
                    var data = response.GetValue<OrchestratorResponse<Session>>();

                    UnityThread.executeInUpdate(() => {
                        _responsesListener?.OnGetSessionInfoResponse(data.ResponseStatus, data.body);
                    });
                }, new { });
            }
        }

        public void DeleteSession(string sessionId) {
            lock (this) {
                _socket.Emit("DeleteSession", (response) => {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();

                    UnityThread.executeInUpdate(() => {
                        _responsesListener?.OnDeleteSessionResponse(data.ResponseStatus);
                    });
                }, new {
                    sessionId
                });
            }
        }

        public void JoinSession(string sessionId) {
            lock (this) {
                _socket.Emit("JoinSession", (response) => {
                    var data = response.GetValue<OrchestratorResponse<Session>>();

                    UnityThread.executeInUpdate(() => {
                        _responsesListener?.OnJoinSessionResponse(data.ResponseStatus, data.body);
                    });
                }, new {
                    sessionId
                });
            }
        }

        public void LeaveSession() {
            lock (this) {
                _socket.Emit("LeaveSession", (response) => {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();

                    UnityThread.executeInUpdate(() => {
                        _responsesListener?.OnLeaveSessionResponse(data.ResponseStatus);
                    });
                }, new { });
            }
        }

        public void SendMessage(string message, string userId) {
            lock (this) {
                _socket.Emit("SendMessage", (response) => {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();

                    UnityThread.executeInUpdate(() => {
                        _responsesListener?.OnSendMessageResponse(data.ResponseStatus);
                    });
                }, new {
                    message,
                    userId
                });
            }
        }

        public void SendMessageToAll(string message) {
            lock (this) {
                _socket.Emit("SendMessageToAll", (response) => {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();

                    UnityThread.executeInUpdate(() => {
                        _responsesListener?.OnSendMessageResponse(data.ResponseStatus);
                    });
                }, new {
                    message
                });
            }
        }

        public void UpdateUserDataJson(UserData userData) {
            lock (this) {
                _socket.Emit("UpdateUserDataJson", (response) => {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();

                    UnityThread.executeInUpdate(() => {
                        _responsesListener?.OnUpdateUserDataJsonResponse(data.ResponseStatus);
                    });
                }, new {
                    userDataJson = userData.AsJsonString()
                });
            }
        }

        #endregion

        #region scene events

        public void SendSceneEventPacketToMaster(byte[] pByteArray) {
            lock (_sendLock) {
                _socket.Emit("SendSceneEventToMaster",
                    pByteArray
                );
            }
        }

        public void SendSceneEventPacketToUser(string pUserID, byte[] pByteArray) {
            lock (_sendLock) {
                _socket.Emit("SendSceneEventToUser",
                    pUserID, pByteArray
                );
            }
        }

        public void SendSceneEventPacketToAllUsers(byte[] pByteArray) {
            lock (_sendLock) {
                _socket.Emit("SendSceneEventToAllUsers",
                    pByteArray
                );
            }
        }

        public void SendBroadcastToChannel(string channel, byte[] pByteArray) {
            lock (_sendLock) {
                _socket.Emit("Broadcast",
                    channel,
                    pByteArray
                );
            }
        }

        #endregion

        #region data streams

        public void DeclareDataStream(string pDataStreamType) {
            lock (this) {
                _socket.Emit("DeclareDataStream", pDataStreamType);
            }
        }

        public void RemoveDataStream(string pDataStreamType) {
            lock (this) {
                _socket.Emit("RemoveDataStream", pDataStreamType);
            }
        }

        public void RegisterForDataStream(string pDataStreamUserId, string pDataStreamType) {
            lock (this) {
                _socket.Emit("RegisterForDataStream", pDataStreamUserId, pDataStreamType);
            }
        }

        public void UnregisterFromDataStream(string pDataStreamUserId, string pDataStreamType) {
            lock (this) {
                _socket.Emit("UnregisterFromDataStream", pDataStreamUserId, pDataStreamType);
            }
        }

        public void SendData(string pDataStreamType, byte[] pDataStreamBytes) {
            lock (this) {
                _socket.Emit("SendData", pDataStreamType, pDataStreamBytes);
            }
        }

        #endregion

        #region events

        private void OnMessageSentFromOrchestrator(SocketIOResponse response) {
            lock (this) {
                var message = response.GetValue<UserMessage>();
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

        private void OnMasterEventReceived(SocketIOResponse response) {
            lock (this) {
                if (_userMessagesListener != null)
                {
                    var sceneEvent = response.GetValue<SceneEvent>();
                    string data = Encoding.ASCII.GetString(response.InComingBytes[0], 0, response.InComingBytes[0].Length);

                    UnityThread.executeInUpdate(() =>
                    {
                        _userMessagesListener.OnMasterEventReceived(new UserEvent(sceneEvent.sceneEventFrom, data));
                    });
                }
                else {
                    Debug.LogWarning("No UserMessagesListener");
                }
            }
        }

        private void OnUserEventReceived(SocketIOResponse response) {
            lock (this) {
                if (_userMessagesListener != null) {
                    var sceneEvent = response.GetValue<SceneEvent>();
                    string data = Encoding.ASCII.GetString(response.InComingBytes[0], 0, response.InComingBytes[0].Length);


                    UnityThread.executeInUpdate(() =>
                    {
                        _userMessagesListener.OnUserEventReceived(new UserEvent(sceneEvent.sceneEventFrom, data));
                    });
                } else {
                    Debug.LogWarning("No UserMessagesListener");
                }
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

        private void OnSessionUpdated(SocketIOResponse response) {
            lock (this) {
                var data = response.GetValue<SessionUpdate>();

                if (data.eventData.userId == _myUserID) {
                    return;
                }

                switch (data.eventId) {
                    case "USER_JOINED_SESSION":
                        foreach (IUserSessionEventsListener e in _userSessionEventslisteners)
                        {
                            UnityThread.executeInUpdate(() => {
                                e?.OnUserJoinedSession(data.eventData.userId, data.eventData.userData);
                            });
                        }
                        break;
                    case "USER_LEFT_SESSION":
                        foreach (IUserSessionEventsListener e in _userSessionEventslisteners)
                        {
                            UnityThread.executeInUpdate(() => {
                                e?.OnUserLeftSession(data.eventData.userId);
                            });
                        }
                        break;
                    case "USER_RAISED_HAND":
                        foreach (IUserSessionEventsListener e in _userSessionEventslisteners)
                        {
                            UnityThread.executeInUpdate(() => {
                                e?.OnUserRaisedHand(data.eventData.userId);
                            });
                        }
                        break;
                    case "USER_CLEARED_RAISED_HAND":
                        foreach (IUserSessionEventsListener e in _userSessionEventslisteners)
                        {
                            UnityThread.executeInUpdate(() => {
                                e?.OnUserClearedRaisedHand(data.eventData.userId);
                            });
                        }
                        break;
                }
            }
        }

        #endregion
    }
}
