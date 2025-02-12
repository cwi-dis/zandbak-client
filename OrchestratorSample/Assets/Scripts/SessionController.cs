using UnityEngine;
using System;
using System.Collections.Generic;
using Orchestrator.Wrapping;
using Orchestrator.Elements;
using Orchestrator.Responses;

public class SessionController : MonoBehaviour
{
    public string OrchestratorURL = "";
    public GameObject PlayerPrefab;

    private Dictionary<string, GameObject> activeUsers = new Dictionary<string, GameObject>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        OrchestratorController.Instance.SocketConnect(OrchestratorURL);

        OrchestratorController.Instance.OnConnectionEvent += OnOrchestratorConnected;
        OrchestratorController.Instance.OnLoginEvent += OnLoginComplete;
        OrchestratorController.Instance.OnSessionsEvent += OnGetSessions;
        OrchestratorController.Instance.OnAddSessionEvent += OnSessionReady;
        OrchestratorController.Instance.OnUserJoinSessionEvent += OnUserJoined;
        OrchestratorController.Instance.OnUserLeaveSessionEvent += OnUserLeft;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnOrchestratorConnected(bool connected) {
        OrchestratorController.Instance.Login(Guid.NewGuid().ToString());
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
    }

    void OnUserJoined(string userId) {
        var newPlayer = Instantiate(PlayerPrefab);
        var networkBehaviour = newPlayer.GetComponent<PlayerNetworkBehaviour>();
        networkBehaviour.id = userId;

        Debug.Log("Spawning new user with id " + userId);
        activeUsers.Add(userId, newPlayer);
    }

    void OnUserLeft(string userId) {
        GameObject obj;

        if (activeUsers.TryGetValue(userId, out obj)) {
            Destroy(obj);
        }
    }
}
