#pragma warning disable CS0618

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Orchestrator.Data;
using Orchestrator.Wrapping;
using UnityEngine;

namespace Orchestrator.App
{
    public class Session
    {
        private readonly Orchestrator _orchestrator;
        private Data.Session _sessionData;

        public Data.Session SessionData
        {
            set => _sessionData = value;
        }

        public string Id => _sessionData.Id;
        public string Name => _sessionData.Name;
        public string Status => _sessionData.Status;
        public string Description => _sessionData.Description;
        public bool Persistent => _sessionData.Persistent;
        public bool IsJoined => _orchestrator.CurrentSession?.Id == Id;

        public List<Presentation> Presentations => _sessionData.Presentations.ToList();
        public Presentation CurrentPresentation;
        public bool IsSharing => CurrentPresentation.IsSharing;

        public List<Bubble> Bubbles { get; private set; }
        public Bubble CurrentBubble;

        public Room Room { get; private set; }

        public List<ChatMessage> Chat => _sessionData.Chat.ToList();
        public Dictionary<string, List<ChatMessage>> PrivateMessages = new();

        public List<User> RaisedHands => _sessionData.RaisedHands.Select(u => FindUserById(u.Id)).ToList();

        public List<User> Users { get; private set; }
        public User Master => Users.Find((u) => u.Id == _sessionData.MasterId);
        public List<User> VRUsers => Users.FindAll((u) => u.DeviceType == "vr");
        public List<User> ARUsers => Users.FindAll((u) => u.DeviceType == "ar");

        public SelfUser Self => _orchestrator.Self;
        public List<User> Speakers => Users.Where(u => u.IsSpeaking).ToList();

        /// <summary>
        /// Occurs when the session is being closed remotely
        /// </summary>
        /// <remarks>
        /// This event is triggered whenever the current session is closed on the Orchestrator.
        /// </remarks>
        public event Action OnClosed;

        /// <summary>
        /// Occurs when a user joins the current session.
        /// </summary>
        /// <remarks>
        /// This event is triggered whenever a new user is added to the session. The event provides
        /// the user who joined as an argument, allowing subscriber methods to access the user's data.
        /// </remarks>
        public event Action<User> OnUserJoined;

        /// <summary>
        /// Occurs when a user leaves the current session. This event can also be triggered with the
        /// current user as an argument. This has occurred if the second parameter is true. This means
        /// that the current user has been removed from the session by an administrator. In this case,
        /// the current user is responsible for cleaning up their local session and loading a different
        /// scene.
        /// </summary>
        /// <remarks>
        /// This event is triggered whenever a user is removed from the session. The event provides
        /// the user who left as an argument, allowing subscriber methods to access the user's data.
        /// The second parameter indicates whether the user left the session themselves (false) or
        /// was removed by an admin (true).
        /// </remarks>
        public event Action<User, bool> OnUserLeft;

        /// <summary>
        /// Occurs when a user's status changes within the session.
        /// </summary>
        /// <remarks>
        /// This event is triggered whenever there is a change in a user's status.
        /// </remarks>
        public event Action<User> OnUserStatusChanged;

        /// <summary>
        /// Occurs when the current presentation in the session is updated or changed.
        /// </summary>
        /// <remarks>
        /// This event is triggered whenever the active presentation for a session is modified.
        /// Subscriber methods receive the updated presentation object as an argument,
        /// allowing them to respond to changes in the active presentation.
        /// </remarks>
        public event Action<Presentation> OnPresentationChanged;

        /// <summary>
        /// Occurs when the current presentation in the session changes it's <c>isSharing</c> flag.
        /// </summary>
        /// <remarks>
        /// This event is triggered whenever the active presentation for a session is modified.
        /// Subscriber methods receive the updated presentation object as an argument,
        /// allowing them to respond to changes in the active presentation.
        /// </remarks>
        public event Action<Presentation> OnPresentationIsSharingChanged;

        /// <summary>
        /// Occurs when the current presentation's slide in the session is updated or changed.
        /// </summary>
        /// <remarks>
        /// This event is triggered whenever the slide of the current presentation for a session is modified.
        /// Subscriber methods receive the updated presentation object as an argument,
        /// allowing them to respond to changes in the active presentation.
        /// </remarks>
        public event Action<Presentation> OnPresentationSlideChanged;

        /// <summary>
        /// Occurs when the status of the session changes.
        /// </summary>
        /// <remarks>
        /// This event is triggered whenever the session's status is updated. The event provides the new
        /// session status as a string argument, enabling subscriber methods to respond to status changes.
        /// </remarks>
        public event Action<string> OnSessionStatusChanged;

        /// <summary>
        /// Occurs when a user raised their hand
        /// </summary>
        /// <remarks>
        /// This event is triggered whenever a user in the session raises their hand. The event provides the user that
        /// raised their hand as an argument.
        /// </remarks>
        public event Action<User> OnUserRaisedHand;

        /// <summary>
        /// Occurs when a raised hand has been cleared.
        /// </summary>
        /// <remarks>
        /// This event is triggered whenever a user's raised hand in the session is cleared. The event provides the
        /// user that raised their hand as an argument.
        /// </remarks>
        public event Action<User> OnUserClearedRaisedHand;

        /// <summary>
        /// Triggered when a new message is received in the session.
        /// </summary>
        /// <remarks>
        /// This event is invoked whenever a user in the session sends a message. The event provides the message
        /// content as a parameter, allowing subscribed methods to access the message details for processing or display.
        /// </remarks>
        public event Action<ChatMessage> OnMessageReceived;

        /// <summary>
        /// Triggered when a user changes their <c>isSpeaking</c> status
        /// </summary>
        /// <remarks>
        /// This event is invoked whenever a user changes their isSpeaking flag. The event provides the user and the
        /// new value of the flag.
        /// </remarks>
        public event Action<User, bool> OnIsSpeakingChanged;

        /// <summary>
        /// Occurs when broadcast data is received in the session.
        /// </summary>
        /// <remarks>
        /// This event is triggered whenever new broadcast data is received. The event provides the
        /// broadcast data as an argument, allowing subscriber methods to access and process the transmitted data.
        /// </remarks>
        public event Action<BroadcastData> OnBroadcastDataReceived;

        /// <summary>
        /// Occurs when a new bubble invitation is received.
        /// </summary>
        /// <remarks>
        /// This event is triggered whenever the current user is invited to a bubble within the session.
        /// </remarks>
        public event Action<Bubble> OnBubbleInvited;

        /// <summary>
        /// Occurs when a bubble join request is either approved or denied.
        /// </summary>
        /// <remarks>
        /// This event is triggered in response to a bubble join request by the current user
        /// </remarks>
        public event Action<Bubble, bool> OnBubbleJoinRequestApproved;

        public Session(Orchestrator orchestrator, Data.Session sessionData)
        {
            _sessionData = sessionData;
            _orchestrator = orchestrator;

            Users = _sessionData.UserDefinitions.Select(u => new User(orchestrator, u)).ToList();
            Bubbles = _sessionData.Bubbles.Select(b => new Bubble(orchestrator, b)).ToList();
            Room = new Room(orchestrator, _sessionData.Room);

            OrchestratorController.Instance.OnSessionCloseEvent += SessionClosed;

            OrchestratorController.Instance.OnUserJoinSessionEvent += UserJoined;
            OrchestratorController.Instance.OnUserLeaveSessionEvent += UserLeft;

            OrchestratorController.Instance.OnSessionPresentationChangedEvent += PresentationChanged;
            OrchestratorController.Instance.OnSessionPresentationSlideChangedEvent += PresentationSlideChanged;
            OrchestratorController.Instance.OnSessionPresentationIsSharingEvent += PresentationIsSharingChanged;

            OrchestratorController.Instance.OnSessionStatusChangedEvent += SessionStatusChanged;

            OrchestratorController.Instance.OnUserRaisedHandEvent += UserRaisedHand;
            OrchestratorController.Instance.OnUserClearedRaisedHandEvent += UserClearedRaisedHand;

            OrchestratorController.Instance.OnUserMessageReceivedEvent += UserMessageReceived;

            OrchestratorController.Instance.OnSessionIsSpeakingEvent += IsSpeakingChanged;
            OrchestratorController.Instance.OnUserStatusChangedEvent += UserStatusChanged;

            OrchestratorController.Instance.OnBroadcastReceivedEvent += BroadcastReceived;

            OrchestratorController.Instance.OnBubbleInvited += BubbleInvited;
            OrchestratorController.Instance.OnBubbleJoinRequestApproved += BubbleJoinRequestApproved;
        }

        /// <summary>
        /// Retrieves the latest session information from the orchestrator. This includes all session information and
        /// data such as users currently in the session.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result is the updated session object.</returns>
        public Task<Session> Info()
        {
            var tcs = new TaskCompletionSource<Session>();

            OrchestratorController.Instance.Wrapper.GetSessionInfo((_, sessionData) =>
            {
                _sessionData = sessionData;
                tcs.SetResult(this);
            });

            return tcs.Task;
        }

        /// <summary>
        /// Joins the current session by associating the calling user and the orchestrator with the session.
        /// Updates the session state and enables movement broadcast listeners for all users within the session.
        /// </summary>
        public async void Join()
        {
            await Info();

            _orchestrator.CurrentSession = this;
            Self.Join();

            // Update user definitions from refreshed sessionData
            var newUsers = _sessionData.UserDefinitions.Select(u =>
            {
                var foundUser = FindUserById(u.Id);

                if (foundUser != null)
                {
                    foundUser.UserData = u;
                    return foundUser;
                }

                return new User(_orchestrator, u);
            }).ToList();

            Users = newUsers;

            foreach (var user in Users)
            {
                if (user.Id != Self.Id)
                {
                    user.Join();
                }
            }
        }

        /// <summary>
        /// Leaves this session.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result is a boolean indicating whether the session was left successfully.</returns>
        public Task<bool> Leave()
        {
            var tcs = new TaskCompletionSource<bool>();

            OrchestratorController.Instance.Wrapper.LeaveSession((_) =>
            {
                foreach (var user in Users)
                {
                    user.Leave();
                }

                if (_orchestrator.Sessions.Remove(_orchestrator.CurrentSession))
                {
                    Debug.Log("Removed current session from session list");
                }

                _orchestrator.CurrentSession = null;

                tcs.SetResult(true);
            });

            return tcs.Task;
        }

        /// <summary>
        /// Removes a user from the current session. This action can only be performed by the session creator.
        /// </summary>
        /// <param name="userToRemove">The user to be removed from the session.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the user was successfully removed.</returns>
        public Task<bool> RemoveUser(User userToRemove)
        {
            var tcs = new TaskCompletionSource<bool>();

            OrchestratorController.Instance.Wrapper.LeaveSession(userToRemove.Id, (_) =>
            {
                userToRemove.Leave();
                tcs.SetResult(true);
            });

            return tcs.Task;
        }

        /// <summary>
        /// Advances the current session to the next presentation, if available.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result is the next presentation object.</returns>
        public Task<Presentation> GoToNextPresentation()
        {
            var tcs = new TaskCompletionSource<Presentation>();

            OrchestratorController.Instance.Wrapper.GoToNextPresentation((_, presentation) =>
            {
                tcs.SetResult(presentation);
                CurrentPresentation = presentation;
            });

            return tcs.Task;
        }

        /// <summary>
        /// Sets the current presentation to the given index, if available.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result is the new current presentation object.</returns>
        public Task<Presentation> GoToPresentation(int index)
        {
            var tcs = new TaskCompletionSource<Presentation>();

            OrchestratorController.Instance.Wrapper.GoToPresentation(index, (_, presentation) =>
            {
                tcs.SetResult(presentation);
                CurrentPresentation = presentation;
            });

            return tcs.Task;
        }

        /// <summary>
        /// Changes the current slide in the presentation by a specified offset.
        /// </summary>
        /// <param name="slideOffset">The number of slides to move forward or backward. Positive values move forward, while negative values move backward.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is the updated presentation object after the slide has been changed.</returns>
        public Task<Presentation> ChangePresentationSlide(int slideOffset)
        {
            var tcs = new TaskCompletionSource<Presentation>();

            Action<Presentation> fn = null;
            fn = (presentation) =>
            {
                tcs.SetResult(presentation);
                CurrentPresentation.CurrentSlide = presentation.CurrentSlide;
                OrchestratorController.Instance.OnSessionPresentationSlideChangedEvent -= fn;
            };

            OrchestratorController.Instance.OnSessionPresentationSlideChangedEvent += fn;
            OrchestratorController.Instance.ChangeSlide(slideOffset);

            return tcs.Task;
        }

        /// <summary>
        /// Sets the current slide in the presentation to the given index.
        /// </summary>
        /// <param name="slideIndex">The index of the slide to set the current slide to</param>
        /// <returns>A task that represents the asynchronous operation. The task result is the updated presentation object after the slide has been changed.</returns>
        public Task<Presentation> SetPresentationSlide(int slideIndex)
        {
            var tcs = new TaskCompletionSource<Presentation>();

            Action<Presentation> fn = null;
            fn = (presentation) =>
            {
                tcs.SetResult(presentation);
                CurrentPresentation.CurrentSlide = presentation.CurrentSlide;
                OrchestratorController.Instance.OnSessionPresentationSlideChangedEvent -= fn;
            };

            OrchestratorController.Instance.OnSessionPresentationSlideChangedEvent += fn;
            OrchestratorController.Instance.SetSlide(slideIndex);

            return tcs.Task;
        }

        /// <summary>
        /// Toggles the sharing state of the current presentation in the session.
        /// </summary>
        /// <param name="isSharing">A boolean indicating whether to start (true) or stop (false) sharing the current presentation.</param>
        /// <returns>A task representing the asynchronous operation. The task result is the updated presentation object that reflects the new sharing state.</returns>
        public Task<Presentation> SharePresentation(bool isSharing)
        {
            var tcs = new TaskCompletionSource<Presentation>();

            Action<Presentation> fn = null;
            fn = (presentation) =>
            {
                tcs.SetResult(presentation);
                CurrentPresentation.IsSharing = presentation.IsSharing;
                OrchestratorController.Instance.OnSessionPresentationIsSharingEvent -= fn;
            };

            OrchestratorController.Instance.OnSessionPresentationIsSharingEvent += fn;
            OrchestratorController.Instance.SetCurrentPresentationIsSharing(isSharing);

            return tcs.Task;
        }

        /// <summary>
        /// Updates the current session status in the Orchestrator.
        /// </summary>
        /// <param name="sessionStatus">The new status to set for the current session.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated session status as a string.</returns>
        public Task<string> SetSessionStatus(string sessionStatus)
        {
            var tcs = new TaskCompletionSource<string>();

            Action<string> fn = null;
            fn = (status) =>
            {
                tcs.SetResult(status);
                OrchestratorController.Instance.OnSessionStatusChangedEvent -= fn;
            };

            OrchestratorController.Instance.OnSessionStatusChangedEvent += fn;
            OrchestratorController.Instance.ChangeSessionStatus(sessionStatus);

            return tcs.Task;
        }

        /// <summary>
        /// Retrieves a list of users who have currently raised their hands in the session.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of users with raised hands.</returns>
        public Task<List<User>> GetRaisedHands()
        {
            var tcs = new TaskCompletionSource<List<User>>();

            Action<List<Data.User>> fn = null;
            fn = (users) =>
            {
                _sessionData.RaisedHands = users;
                tcs.SetResult(RaisedHands);

                OrchestratorController.Instance.OnGetRaisedHandsEvent -= fn;
            };

            OrchestratorController.Instance.OnGetRaisedHandsEvent += fn;
            OrchestratorController.Instance.GetRaisedHands();

            return tcs.Task;
        }

        /// <summary>
        /// Clears the raised hand status of a user in the session. This will trigger an event
        /// when the operation is completed.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose raised hand status is to be cleared.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the operation was successful.</returns>
        public Task<bool> ClearRaisedHand(string userId)
        {
            var tcs = new TaskCompletionSource<bool>();

            Action fn = null;
            fn = async () =>
            {
                await GetRaisedHands();
                tcs.SetResult(true);
                OrchestratorController.Instance.OnClearRaisedHandEvent -= fn;
            };

            OrchestratorController.Instance.OnClearRaisedHandEvent += fn;
            OrchestratorController.Instance.ClearRaisedHand(userId);

            return tcs.Task;
        }

        /// <summary>
        /// Sends a given chat message to all users in the current session.
        /// </summary>
        /// <param name="message">Message to be sent</param>
        public Task<bool> SendMessage(string message)
        {
            var tcs = new TaskCompletionSource<bool>();

            OrchestratorController.Instance.SendMessageToAll(message);
            tcs.SetResult(true);

            return tcs.Task;
        }

        /// <summary>
        /// Retrieves the chat messages for the current session.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of chat messages.</returns>
        public Task<List<ChatMessage>> GetChatMessages()
        {
            var tcs = new TaskCompletionSource<List<ChatMessage>>();

            Action<List<ChatMessage>> fn = null;
            fn = (messages) =>
            {
                _sessionData.Chat = messages;
                tcs.SetResult(Chat);

                OrchestratorController.Instance.OnGetMessagesEvent -= fn;
            };

            OrchestratorController.Instance.OnGetMessagesEvent += fn;
            OrchestratorController.Instance.GetMessages();

            return tcs.Task;
        }

        /// <summary>
        /// Creates a new conversation bubble within the session.
        /// </summary>
        /// <param name="name">The name of the bubble to be created.</param>
        /// <returns>A task representing the asynchronous operation. The task result contains the newly created bubble.</returns>
        public Task<Bubble> CreateBubble(string name = null)
        {
            var tcs = new TaskCompletionSource<Bubble>();

            OrchestratorController.Instance.Wrapper.CreateBubble(name, (_, body) =>
            {
                var bubble = new Bubble(_orchestrator, body);
                Bubbles.Add(bubble);
                CurrentBubble = bubble;

                tcs.SetResult(bubble);
            });

            return tcs.Task;
        }

        /// <summary>
        /// Retrieves the list of currently available bubbles within the session. Updates the local bubble list and
        /// returns the fetched bubbles.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result is the list of updated bubbles.</returns>
        public Task<List<Bubble>> ListBubbles()
        {
            var tcs = new TaskCompletionSource<List<Bubble>>();

            OrchestratorController.Instance.Wrapper.ListBubbles((_, body) =>
            {
                Bubbles = body.Select(b => new Bubble(_orchestrator, b)).ToList();
                tcs.SetResult(Bubbles);
            });

            return tcs.Task;
        }

        /// <summary>
        /// Sends a request to join the specified bubble in the session to the owner of the bubble. If an error occurs,
        /// an exception is raised.
        /// </summary>
        /// <param name="bubble">The bubble to join</param>
        /// <returns>A task representing the asynchronous operation. The task result is a boolean value indicating the success of the join request.</returns>
        public Task<bool> RequestBubbleJoin(Bubble bubble)
        {
            var tcs = new TaskCompletionSource<bool>();

            OrchestratorController.Instance.Wrapper.RequestBubbleJoin(bubble.Id, (status) =>
            {
                if (status.Error == 0)
                {
                    tcs.SetResult(true);
                }
                else
                {
                    tcs.SetException(new Exception(status.Message));
                }
            });

            return tcs.Task;
        }

        /// <summary>
        /// Retrieves a bubble from the session's bubble list based on the specified bubble ID. Returns the bubble
        /// object if found, or raised an exception otherwise.
        /// </summary>
        /// <param name="bubbleId">The ID of the bubble to retrieve</param>
        /// <returns>A bubble object corresponding to the given ID</returns>
        public Task<Bubble> GetBubble(string bubbleId)
        {
            var tcs = new TaskCompletionSource<Bubble>();

            OrchestratorController.Instance.Wrapper.GetBubble(bubbleId, (status, body) =>
            {
                if (status.Error == 0)
                {
                    Bubbles = Bubbles.Select(b => b.Id == bubbleId ? new Bubble(_orchestrator, body) : b).ToList();
                    tcs.SetResult(Bubbles.Find(b => b.Id == bubbleId));
                }
                else
                {
                    tcs.SetException(new Exception(status.Message));
                }
            });

            return tcs.Task;
        }

        /// <summary>
        /// Joins the specified bubble in response to an invitation from another user. If the current user has not been
        /// invited to the specified bubble, this method will raise an exception.
        /// </summary>
        /// <param name="bubble">The bubble to join, representing the virtual space or group.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is a boolean indicating whether the join operation was successful.</returns>
        public Task<bool> JoinBubble(Bubble bubble)
        {
            var tcs = new TaskCompletionSource<bool>();

            OrchestratorController.Instance.Wrapper.JoinBubble(bubble.Id, async (status) =>
            {
                if (status.Error == 0)
                {
                    var refreshedBubble = await GetBubble(bubble.Id);
                    CurrentBubble = refreshedBubble;

                    tcs.SetResult(true);
                }
                else
                {
                    tcs.SetException(new Exception(status.Message));
                }
            });

            return tcs.Task;
        }

        /// <summary>
        /// Broadcasts an object containing transform data to all users in the current session.
        /// </summary>
        /// <param name="data">The transform data object to be broadcast to the session.</param>
        /// <typeparam name="T">The type of the data object being broadcast.</typeparam>
        public void BroadcastTransform<T>(T data)
        {
            OrchestratorController.Instance.Broadcast("transform", JsonConvert.SerializeObject(data));
        }

        /// <summary>
        /// Retrieves a user from the session's user list based on the provided user ID.
        /// </summary>
        /// <param name="userId">The unique identifier of the user to retrieve.</param>
        /// <returns>The user object matching the specified ID, or null if no matching user is found.</returns>
        public User FindUserById(string userId)
        {
            return Users.Find(u => u.Id == userId);
        }

        #region events

        private void UserJoined(string userId, Data.User userData)
        {
            var joinedUser = new User(_orchestrator, userData);
            joinedUser.Join();

            if (Users.Find(u => u.Id == userId) == null)
            {
                Users.Add(joinedUser);
            }

            OnUserJoined?.Invoke(joinedUser);
        }

        private void UserLeft(string userId, bool force)
        {
            var userToRemove = Users.Find(user => user.Id == userId);

            if (userToRemove != null)
            {
                userToRemove.Leave();
                Users.Remove(userToRemove);

                if (userId == Self.Id)
                {
                    _orchestrator.CurrentSession = null;
                }

                OnUserLeft?.Invoke(userToRemove, force);
            }
        }

        private void UserStatusChanged(Data.User user, string status)
        {
            var foundUser = Users.Find(u => u.Id == user.Id);
            foundUser.UserData.Status = status;

            OnUserStatusChanged?.Invoke(foundUser);
        }

        private async void SessionClosed()
        {
            await Leave();
            OnClosed?.Invoke();
        }

        private void PresentationChanged(Presentation presentation)
        {
            CurrentPresentation = presentation;
            OnPresentationChanged?.Invoke(presentation);
        }

        private void PresentationSlideChanged(Presentation presentation)
        {
            CurrentPresentation.CurrentSlide = presentation.CurrentSlide;
            OnPresentationSlideChanged?.Invoke(presentation);
        }

        private void PresentationIsSharingChanged(Presentation presentation)
        {
            CurrentPresentation.IsSharing = presentation.IsSharing;
            OnPresentationIsSharingChanged?.Invoke(presentation);
        }

        private void SessionStatusChanged(string status)
        {
            _sessionData.Status = status;
            OnSessionStatusChanged?.Invoke(status);
        }

        private async void UserRaisedHand(string userId)
        {
            await GetRaisedHands();

            var raisedHandUser = FindUserById(userId);
            raisedHandUser.UserData.HasHandRaised = true;

            OnUserRaisedHand?.Invoke(raisedHandUser);
        }

        private void UserClearedRaisedHand(string userId)
        {
            GetRaisedHands();

            var clearedRaisedHandUser = Users.Find(u => u.Id == userId);
            clearedRaisedHandUser.UserData.HasHandRaised = false;

            OnUserClearedRaisedHand?.Invoke(clearedRaisedHandUser);
        }

        private void UserMessageReceived(ChatMessage message)
        {
            if (message.Private)
            {
                if (PrivateMessages.TryGetValue(message.Sender.Id, out var privateMessage))
                {
                    privateMessage.Add(message);
                }
                else
                {
                    PrivateMessages.Add(message.Sender.Id, new List<ChatMessage> { message });
                }
            }
            else
            {
                _sessionData.Chat.Add(message);
            }

            OnMessageReceived?.Invoke(message);
        }

        private void IsSpeakingChanged(Data.User user, bool isSpeaking)
        {
            var foundUser = Users.Find(u => u.Id == user.Id);
            if (foundUser == null) return;

            OnIsSpeakingChanged?.Invoke(foundUser, isSpeaking);
        }

        private void BroadcastReceived(BroadcastData data)
        {
            OnBroadcastDataReceived?.Invoke(data);
        }

        private async void BubbleInvited(string bubbleId)
        {
            await ListBubbles();
            var invitedBubble = Bubbles.Find((b) => b.Id == bubbleId);

            if (invitedBubble != null)
            {
                OnBubbleInvited?.Invoke(invitedBubble);
            }
        }

        private async void BubbleJoinRequestApproved(string bubbleId, bool approved)
        {
            var requestedBubble = Bubbles.Find((b) => b.Id == bubbleId);
            if (requestedBubble == null) return;

            if (approved)
            {
                var bubble = await GetBubble(bubbleId);
                CurrentBubble = bubble;
            }

            OnBubbleJoinRequestApproved?.Invoke(requestedBubble, approved);
        }

        #endregion
    }
}

#pragma warning restore CS0618
