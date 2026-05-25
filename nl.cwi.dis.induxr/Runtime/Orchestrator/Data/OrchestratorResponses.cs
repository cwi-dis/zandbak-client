using Newtonsoft.Json;

namespace Orchestrator.Data {
    public interface IOrchestratorResponseBody { }

    // class that describes the status for the response from the orchestrator
    public class ResponseStatus
    {
        public const int Ok = 0;

        public readonly int Error;
        public readonly string Message;

        public ResponseStatus(int error, string message)
        {
            Error = error;
            Message = message;
        }
        public ResponseStatus() : this(0, "OK") { }
    }

    public class OrchestratorResponse<T>
    {
        [JsonProperty("error")] public int Error { get; set; }
        [JsonProperty("message")] public string Message { get; set; }

        [JsonProperty("body")] public T Body;

        public ResponseStatus ResponseStatus => new(Error, Message);
    }

    public class EmptyResponse : IOrchestratorResponseBody {}

    public class VersionResponse : IOrchestratorResponseBody {
        [JsonProperty("orchestratorVersion")] public string OrchestratorVersion;
    }

    public class LoginResponse : IOrchestratorResponseBody {
        [JsonProperty("userId")] public string UserId;
        [JsonProperty("userData")] public User UserData;
    }

    public class PresentationResponse : IOrchestratorResponseBody
    {
        [JsonProperty("sessionId")] public string SessionId;
        [JsonProperty("sessionCurrentPresentation")] public Presentation Presentation;
    }

    public class StatusResponse : IOrchestratorResponseBody
    {
        [JsonProperty("sessionId")] public string SessionId;
        [JsonProperty("sessionStatus")] public string Status;
    }

    public class SessionUpdateUserData {
        [JsonProperty("userId")] public string UserId;
        [JsonProperty("userData")] public User UserData;
    }

    public class SessionUpdateUserStatus {
        [JsonProperty("userId")] public string UserId;
        [JsonProperty("status")] public string Status;
    }

    public class SessionUpdateBubbleId {
        [JsonProperty("bubbleId")] public string BubbleId;
        [JsonProperty("approve")] public bool? Approved;
    }

    public class SessionUpdatePresentationData {
        [JsonProperty("currentPresentation")] public Presentation Presentation;
    }

    public class SessionUpdateIsSpeakingData
    {
        [JsonProperty("isSpeaking")] public bool IsSpeaking;
        [JsonProperty("userId")] public string UserId;
    }

    public class SessionUpdateIsForceData
    {
        [JsonProperty("force")] public bool Force;
        [JsonProperty("userId")] public string UserId;
    }

    public class SessionUpdateStatusData
    {
        [JsonProperty("status")] public string Status;
    }

    public class EmptyUpdate
    {
    }

    public class SessionUpdate<T> {
        [JsonProperty("eventId")] public string EventId;
        [JsonProperty("eventData")] public T EventData;
    }

    public class BubbleUpdate<T>
    {
        [JsonProperty("eventId")] public string EventId;
        [JsonProperty("eventData")] public T EventData;
    }

    public class OrchestratorUpdate<T>
    {
        [JsonProperty("eventId")] public string EventId;
        [JsonProperty("eventData")] public T EventData;
    }

    public class SceneEvent {
        [JsonProperty("sceneEventFrom")] public string SceneEventFrom;
    }

    // class that stores a user data-stream packet incoming from the orchestrator
    public class UserDataStreamPacket
    {
        [JsonProperty("dataStreamUserID")] public string UserId;
        [JsonProperty("dataStreamType")] public string Type;
        [JsonProperty("dataStreamDesc")] public string Description;
        [JsonProperty("dataStreamPacket")] public byte[] Packet;

        public UserDataStreamPacket() { }

        public UserDataStreamPacket(string userId, string type, string description, byte[] packet)
        {
            if (packet == null) return;

            UserId = userId;
            Type = type;
            Description = description;
            Packet = packet;
        }
    }

    // class that stores a user event incoming from the orchestrator
    // necessary new parameters welcomed
    public class UserEvent
    {
        [JsonProperty("sceneEventFrom")] public string SceneEventFrom;
        [JsonProperty("sceneEventData")] public string SceneEventData;

        public UserEvent() { }

        public UserEvent(string fromID, string message)
        {
            SceneEventFrom = fromID;
            SceneEventData = message;
        }
    }

    public class BroadcastData
    {
        [JsonProperty("channel")] public string Channel;
        [JsonProperty("data")] public string Data;

        // Raw byte payload
        public readonly byte[] Bytes;

        public BroadcastData() { }

        public BroadcastData(string channel, string data) {
            Channel = channel;
            Data = data;
        }

        public BroadcastData(string channel, byte[] bytes) {
            Channel = channel;
            Bytes = bytes;
        }
    }
}
