//Interfaces to be implemented to supervise the orchestrator
namespace Orchestrator.Data
{
    public interface IOrchestratorConnectionListener
    {
        void OnSocketConnect();
        void OnSocketConnecting();
        void OnSocketDisconnect();
        void OnSocketError(ResponseStatus message);
    }

    // Interface to implement to listen for user messages emitted spontaneously
    // by the orchestrator
    public interface IUserMessagesListener
    {
        void OnUserMessageReceived(ChatMessage userMessage);
        void OnBroadcastReceived(BroadcastData broadcastData);
    }

    public interface IBubbleEventsListener
    {
        void OnBubbleLeft(User user);
        void OnBubbleJoined(User user);
        void OnBubbleJoinRequested(User user);
    }

    public interface IOrchestratorEventsListener
    {
        void OnSessionCreated(Session session);
        void OnSessionDeleted(Session session);
    }

    // Interface to implement to listen for user events emitted spontaneously
    // from the session updates by the orchestrator
    public interface IUserSessionEventsListener
    {
        void OnSessionClosed();
        void OnUserJoinedSession(string userID, User user);
        void OnUserLeftSession(string userID, bool force);
        void OnUserRaisedHand(string userId);
        void OnUserClearedRaisedHand(string userId);
        void OnSessionStatusChanged(string status);
        void OnUserStatusChanged(string userId, string status);
        void OnPresentationChanged(Presentation presentation);
        void OnPresentationIsSharingChanged(Presentation presentation);
        void OnSlideChanged(Presentation presentation);
        void OnSessionIsSpeakingChanged(string userId, bool isSpeaking);
        void OnBubbleJoinRequestApproved(string bubbleId, bool approved);
        void OnBubbleInvited(string bubbleId);
        void OnTriggerRegistered(Trigger trigger);
        void OnObjectRegistered(SharedObject sharedObject);
        void OnObjectSpawned(SharedObject sharedObject);
        void OnObjectOwnershipChanged(SharedObject sharedObject);
    }

    // Interface for clients that will use the orchestrator wrapper
    // each function is the response of a command and contains the data returned by the orchestrator
    // functions are called by the wrapper upon the response of the orchestrator
    public interface IOrchestratorResponsesListener
    {
        void OnError(ResponseStatus status);
        void OnConnect();
        void OnConnecting();
        void OnDisconnect();
    }
}
