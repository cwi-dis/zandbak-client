using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace Orchestrator.Data
{
    // Base class for the elements returned by the orchestrator
    public class OrchestratorElement
    {
        // used to retrieve the ID
        public virtual string GetId()
        {
            return string.Empty;
        }

        //used to display the element for Gui
        public virtual string GetGuiRepresentation()
        {
            return string.Empty;
        }

        public static T ParseJsonString<T>(string data)
        {
            return JsonUtility.FromJson<T>(data);
        }

        public string AsJsonString()
        {
            return JsonUtility.ToJson(this);
        }
    }

    public class UserPosition : OrchestratorElement
    {
        [JsonProperty("x")] public float X;
        [JsonProperty("y")] public float Y;
        [JsonProperty("z")] public float Z;
    }

    public class UserQuaternion : OrchestratorElement
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

    public class UserTransform : OrchestratorElement
    {
        [JsonProperty("timestamp")] public float Timestamp;
        [JsonProperty("position")] public UserPosition Position;
        [JsonProperty("rotation")] public UserQuaternion Rotation;
        [JsonProperty("bones")] public Dictionary<string, UserBoneData> Bones;
    }

    public class User: OrchestratorElement
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

        public override string GetId()
        {
            return Id;
        }

        public override string GetGuiRepresentation()
        {
            return Username;
        }
    }

    public class UserData: OrchestratorElement
    {
        [JsonProperty("userAudioUrl")] public string UserAudioUrl = "";

        [JsonProperty("webcamName")] public string WebcamName = "";
        [JsonProperty("microphoneName")] public string MicrophoneName = "";

        [JsonProperty("userRepresentationType")] public string UserRepresentationType = "";
    }

    public class SfuData : OrchestratorElement
    {
        [JsonProperty("url_gen")] public string URLGen = "";
        [JsonProperty("url_audio")] public string URLAudio = "";
        [JsonProperty("url_pcc")] public string URLPcc = "";
    }

    public class DataStream : OrchestratorElement
    {
        [JsonProperty("dataStreamUserId")] public string UserId = "";
        [JsonProperty("dataStreamKind")] public string StreamKind = "";
        [JsonProperty("dataStreamDescription")] public string Description = "";
    }

    public class NtpClock: OrchestratorElement
    {
        [JsonProperty("ntpDate")] public string NtpDate;
        [JsonProperty("ntpTimeMs")] public long NtpTimeMs;

        public double Timestamp => NtpTimeMs / 1000.0;
    }

    public class ChatMessage : OrchestratorElement
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("sender")] public User Sender;
        [JsonProperty("message")] public string Message;
        [JsonProperty("timestamp")] public string Timestamp;
    }

    public class Presentation : OrchestratorElement
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("description")] public string Description;
        [JsonProperty("presenter")] public string Presenter;
        [JsonProperty("slidesUrl")] public string SlidesURL;
        [JsonProperty("currentSlide")] public int CurrentSlide;
        [JsonProperty("numSlides")] public int NumSlides;
        [JsonProperty("isSharing")] public bool IsSharing;
    }

    public class ScheduledPresentation : OrchestratorElement
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("description")] public string Description;
        [JsonProperty("presenter")] public string Presenter;
        [JsonProperty("slidesUrl")] public string SlidesURL;
        [JsonProperty("createdAt")] public DateTime CreatedAt;
        [JsonProperty("updatedAt")] public DateTime UpdatedAt;
    }

    public class ScheduledSession : OrchestratorElement
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
    }

    public class Session : OrchestratorElement
    {
        [JsonProperty("sessionId")] public string Id;
        [JsonProperty("sessionName")] public string Name;
        [JsonProperty("sessionDescription")] public string Description;
        [JsonProperty("sessionAdministrator")] public string AdministratorId;
        [JsonProperty("sessionMaster")] public string MasterId;
        [JsonProperty("scenarioId")] public string ScenarioId;
        [JsonProperty("sessionUsers")] public List<string> UserIds;
        [JsonProperty("sessionUserDefinitions")] public List<User> UserDefinitions;
        [JsonProperty("sessionProtocol")] public string Protocol;
        [JsonProperty("sessionChannels")] public List<string> Channels;
        [JsonProperty("sessionChat")] public List<ChatMessage> Chat;
        [JsonProperty("sessionRaisedHands")] public List<User> RaisedHands;
        [JsonProperty("sessionCurrentPresentation")] public Presentation CurrentPresentation;
        [JsonProperty("sessionPresentations")] public List<Presentation> Presentations;
        [JsonProperty("sessionStatus")] public string Status;

        public override string GetId()
        {
            return Id;
        }

        public override string GetGuiRepresentation()
        {
            return Name + " (" + Description + ")";
        }

        public List<User> GetUsers()
        {
            return UserDefinitions;
        }

        public User GetUser(string userID)
        {
            foreach(var userDefinition in UserDefinitions)
            {
                if (userDefinition.Id == userID)
                {
                    return userDefinition;
                }
            }
            return null;
        }

        public int GetUserCount()
        {
            return UserDefinitions.Count;
        }
    }
}
