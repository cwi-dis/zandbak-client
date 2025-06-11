using System.Collections.Generic;
using UnityEngine;

namespace Orchestrator.Elements
{
    // Base class for the elements returned by the orchestrator
    public abstract class OrchestratorElement
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
        public float x;
        public float y;
        public float z;
    }

    public class UserQuaternion : OrchestratorElement
    {
        public float x;
        public float y;
        public float z;
        public float w;
    }

    public class UserBoneData
    {
        public UserPosition pos;
        public UserQuaternion rot;
    }

    public class UserTransform : OrchestratorElement
    {
        public string timestamp;
        public UserPosition position;
        public UserQuaternion rotation;
        public Dictionary<string, UserBoneData> bones;
    }

    public class User: OrchestratorElement
    {
        public string userId;
        public string userName;
        public UserData userData;
        public SfuData sfuData;
        public string userType;
        public UserTransform transform;
        public string deviceType;
        public bool hasHandRaised;

        public User()
        {
        }

        public override string GetId()
        {
            return userId;
        }

        public override string GetGuiRepresentation()
        {
            return userName;
        }
    }

    public class UserData: OrchestratorElement
    {

        public string userAudioUrl = "";

        public string webcamName = "";
        public string microphoneName = "";

        public string userRepresentationType = "";

        // empty constructor callled by the JsonData parser
        public UserData() { }
    }

    public class SfuData : OrchestratorElement
    {
        public string url_gen = "";
        public string url_audio = "";
        public string url_pcc = "";

        public SfuData() {}
    }

    public class DataStream : OrchestratorElement
    {
        public string dataStreamUserId = "";
        public string dataStreamKind = "";
        public string dataStreamDescription = "";

        public DataStream() { }
    }

    public class NtpClock: OrchestratorElement
    {
        public string ntpDate;
        public long ntpTimeMs;

        public NtpClock() { }

        public double Timestamp { get { return (ntpTimeMs / 1000.0); } }
    }

    public class Scenario : OrchestratorElement
    {
        public string scenarioId;
        public string scenarioName;
        public string scenarioDescription;

        public Scenario() { }

        public override string GetId()
        {
            return scenarioId;
        }

        public override string GetGuiRepresentation()
        {
            return scenarioName + " (" + scenarioDescription + ")";
        }
    }

    public class ChatMessage : OrchestratorElement
    {
        public string id;
        public User sender;
        public string message;
        public string timestamp;
    }

    public class Session : OrchestratorElement
    {
        public string sessionId;
        public string sessionName;
        public string sessionDescription;
        public string sessionAdministrator;
        public string sessionMaster;
        public string scenarioId; // the scenario ID
        public string[] sessionUsers;
        public List<User> sessionUserDefinitions;
        public string sessionProtocol;
        public string[] sessionChannels;
        public ChatMessage[] sessionChat;
        public User[] sessionRaisedHands;

        public Session() { }

        public override string GetId()
        {
            return sessionId;
        }

        public override string GetGuiRepresentation()
        {
            return sessionName + " (" + sessionDescription + ")";
        }

        public User[] GetUsers()
        {
            return sessionUserDefinitions.ToArray();
        }

        public User GetUser(string userID)
        {
            foreach(var userDefinition in sessionUserDefinitions)
            {
                if (userDefinition.userId == userID)
                {
                    return userDefinition;
                }
            }
            return null;
        }

        public int GetUserCount()
        {
            return sessionUserDefinitions.Count;
        }
    }
}
