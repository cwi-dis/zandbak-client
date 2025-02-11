using UnityEngine;
using Orchestrator.Wrapping;
using Orchestrator.Elements;
using Orchestrator.Responses;

public class SessionController : MonoBehaviour
{
    public string OrchestratorURL = "";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        OrchestratorController.Instance.SocketConnect(OrchestratorURL);

        OrchestratorController.Instance.OnConnectionEvent += OnOrchestratorConnected;
        OrchestratorController.Instance.OnLoginEvent += OnLoginComplete;
        OrchestratorController.Instance.OnSessionsEvent += OnGetSessions;
        OrchestratorController.Instance.OnAddSessionEvent += OnSessionReady;
        OrchestratorController.Instance.OnBroadcastReceivedEvent += OnBroadcast;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnOrchestratorConnected(bool connected) {
        OrchestratorController.Instance.Login("test", "");
    }

    void OnLoginComplete(bool loggedIn) {
        OrchestratorController.Instance.GetSessions();
    }

    void OnGetSessions(Session[] sessions) {
        if (sessions.Length == 0)
        {
            Debug.Log("Creating new session 'test'");
            OrchestratorController.Instance.AddSession("test");
        }
        else
        {
            Debug.Log("Joining existing session with id " + sessions[0].sessionId);
            OrchestratorController.Instance.JoinSession(sessions[0].sessionId);
        }
    }

    void OnSessionReady(Session session) {
        Debug.Log("Session ready:" + session.sessionName);
        OrchestratorController.Instance.Broadcast("transform", "Hello World");

    }

    void OnBroadcast(BroadcastData data) {
        Debug.Log("Broadcast received: " + data.channel);
        Debug.Log(data.data);
    }
}
