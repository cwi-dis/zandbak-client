using UnityEngine;
using System;
using System.Collections.Generic;
using Orchestrator.Wrapping;
using Orchestrator.Elements;
using Orchestrator.Behaviours;

public class SessionController : MonoBehaviour
{
    public string OrchestratorURL = "";
    public GameObject LocalPlayer;
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

    void OnLoginComplete(bool loggedIn, string userId) {
        Debug.Log("Login complete, received user id " + userId);
        var networkBehaviour = LocalPlayer.GetComponent<NetworkBehaviour>();
        networkBehaviour.Id = userId;

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
        networkBehaviour.Id = userId;

        Debug.Log("Spawning new user with id " + userId);
        activeUsers.Add(userId, newPlayer);
    }

    void OnUserLeft(string userId) {
        Debug.Log("User " + userId + "left session");
        GameObject obj;

        if (activeUsers.TryGetValue(userId, out obj))
        {
            Debug.Log("User found, removing and destroying player object");

            activeUsers.Remove(userId);
            Destroy(obj);
        }
        else
        {
            Debug.LogWarning("Could not find object for user with id " + userId);
        }
    }
}
