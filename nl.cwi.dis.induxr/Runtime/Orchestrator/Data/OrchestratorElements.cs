using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Orchestrator.Data
{
    // Base class for the elements returned by the orchestrator
    public interface IOrchestratorElement {}

    public class UserPosition : IOrchestratorElement
    {
        [JsonProperty("x")] public float X;
        [JsonProperty("y")] public float Y;
        [JsonProperty("z")] public float Z;
    }

    public class UserQuaternion : IOrchestratorElement
    {
        [JsonProperty("x")] public float X;
        [JsonProperty("y")] public float Y;
        [JsonProperty("z")] public float Z;
        [JsonProperty("w")] public float W;
    }

    public class UserBoneData
    {
        [JsonProperty("pos")] public UserPosition Pos;
        [JsonProperty("rot")] public UserQuaternion Rot;
    }

    public class ObjectTransform : IOrchestratorElement
    {
        [JsonProperty("timestamp")] public float Timestamp;
        [JsonProperty("position")] public UserPosition Position;
        [JsonProperty("rotation")] public UserQuaternion Rotation;
    }

    public class UserTransform : ObjectTransform
    {
        [JsonProperty("transforms")] public Dictionary<string, UserBoneData> Transforms;
    }

    public class User: IOrchestratorElement
    {
        [JsonProperty("userId")] public string Id;
        [JsonProperty("userName")] public string Username;
        [JsonProperty("userPassword")] public string Password;
        [JsonProperty("userData")] public UserData UserData;
        [JsonProperty("sfuData")] public SfuData SfuData;
        [JsonProperty("userType")] public string UserType;
        [JsonProperty("transform")] public UserTransform Transform;
        [JsonProperty("deviceType")] public string DeviceType;
        [JsonProperty("hasHandRaised")] public bool HasHandRaised;
        [JsonProperty("isSpeaking")] public bool IsSpeaking;
        [JsonProperty("status")] public string Status;
        [JsonProperty("prefabName")] public string PrefabName;
    }

    public class UserData: IOrchestratorElement
    {
        [JsonProperty("userAudioUrl")] public string UserAudioUrl = "";

        [JsonProperty("webcamName")] public string WebcamName = "";
        [JsonProperty("microphoneName")] public string MicrophoneName = "";

        [JsonProperty("userRepresentationType")] public string UserRepresentationType = "";
    }

    public class SharedObject : IOrchestratorElement
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("owner")] public User Owner;
        [JsonProperty("transform")] public ObjectTransform Transform;
        [JsonProperty("dynamic")] public bool Dynamic;
        [JsonProperty("prefabName")] [CanBeNull] public string PrefabName;
    }

    public class Trigger : IOrchestratorElement
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("owner")] public User Owner;
        [JsonProperty("value")] public JObject Value;
    }

    public class SfuData : IOrchestratorElement
    {
        [JsonProperty("url_gen")] public string URLGen = "";
        [JsonProperty("url_audio")] public string URLAudio = "";
        [JsonProperty("url_pcc")] public string URLPcc = "";
    }

    public class DataStream : IOrchestratorElement
    {
        [JsonProperty("dataStreamUserId")] public string UserId = "";
        [JsonProperty("dataStreamKind")] public string StreamKind = "";
        [JsonProperty("dataStreamDescription")] public string Description = "";
    }

    public class NtpClock: IOrchestratorElement
    {
        [JsonProperty("ntpDate")] public string NtpDate;
        [JsonProperty("ntpTimeMs")] public long NtpTimeMs;

        public double Timestamp => NtpTimeMs / 1000.0;
    }

    public class ChatMessage : IOrchestratorElement
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("sender")] public User Sender;
        [JsonProperty("message")] public string Message;
        [JsonProperty("timestamp")] public string Timestamp;
        [JsonProperty("private")] public bool Private;
    }

    public class Presentation : IOrchestratorElement
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("name")] public string Name;
        [JsonProperty("description")] public string Description;
        [JsonProperty("presenter")] public string Presenter;
        [JsonProperty("slidesUrl")] public string SlidesURL;
        [JsonProperty("currentSlide")] public int CurrentSlide;
        [JsonProperty("numSlides")] public int NumSlides;
        [JsonProperty("isSharing")] public bool IsSharing;
    }

    public class ScheduledPresentation : IOrchestratorElement
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("description")] public string Description;
        [JsonProperty("presenter")] public string Presenter;
        [JsonProperty("slidesUrl")] public string SlidesURL;
        [JsonProperty("createdAt")] public DateTime CreatedAt;
        [JsonProperty("updatedAt")] public DateTime UpdatedAt;
    }

    public class Bubble : IOrchestratorElement
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("name")] public string Name;
        [JsonProperty("owner")] public User Owner;
        [JsonProperty("users")] public List<User> Users;
    }

    public class Room : IOrchestratorElement
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("name")] public string Name;
        [JsonProperty("description")] public string Description;
        [JsonProperty("model")] public string Model;
    }

    public class ScheduledSession : IOrchestratorElement
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("title")] public string Title;
        [JsonProperty("description")] public string Description;
        [JsonProperty("moderator")] public string Moderator;
        [JsonProperty("status")] public string Status;
        [JsonProperty("startTime")] public DateTime Start;
        [JsonProperty("endTime")] public DateTime End;
        [JsonProperty("createdAt")] public DateTime CreatedAt;
        [JsonProperty("updatedAt")] public DateTime UpdatedAt;
        [JsonProperty("presentations")] public List<ScheduledPresentation> Presentations;
        [JsonProperty("room")] public Room Room;
    }

    public class Session : IOrchestratorElement
    {
        [JsonProperty("sessionId")] public string Id;
        [JsonProperty("sessionName")] public string Name;
        [JsonProperty("sessionDescription")] public string Description;
        [JsonProperty("sessionAdministrator")] public string AdministratorId;
        [JsonProperty("sessionMaster")] public string MasterId;
        [JsonProperty("scenarioId")] public string ScenarioId;
        [JsonProperty("sessionUsers")] public List<string> UserIds;
        [JsonProperty("sessionUserDefinitions")] public List<User> UserDefinitions;
        [JsonProperty("sessionObjects")] public List<SharedObject> SharedObjects;
        [JsonProperty("sessionTriggers")] public List<Trigger> Triggers;
        [JsonProperty("sessionProtocol")] public string Protocol;
        [JsonProperty("sessionChannels")] public List<string> Channels;
        [JsonProperty("sessionChat")] public List<ChatMessage> Chat;
        [JsonProperty("sessionRaisedHands")] public List<User> RaisedHands;
        [JsonProperty("sessionCurrentPresentation")] public Presentation CurrentPresentation;
        [JsonProperty("sessionPresentations")] public List<Presentation> Presentations;
        [JsonProperty("sessionBubbles")] public List<Bubble> Bubbles;
        [JsonProperty("sessionRoom")] public Room Room;
        [JsonProperty("sessionStatus")] public string Status;
        [JsonProperty("sessionPersistent")] public bool Persistent;
    }
}
