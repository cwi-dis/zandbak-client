using System;
using System.Linq;
using Orchestrator.Elements;
using Orchestrator.Wrapping;
using TMPro;
using UnityEngine;

public class SessionSelector : MonoBehaviour
{
    public TMP_Dropdown sessionDropdown;
    private Session[] _sessions;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        OrchestratorController.Instance.OnSessionsEvent = OnGetSessions;
        OrchestratorController.Instance.GetSessions();
    }

    private void OnGetSessions(Session[] sessions)
    {
        sessionDropdown.AddOptions(sessions.Select((s) => s.sessionName).ToList());
        _sessions = sessions;
    }

    public void OnJoinSession()
    {
        var selectedDropdownValue = sessionDropdown.value;
        OrchestratorController.Instance.OnJoinSessionEvent = OnSessionJoined;
        OrchestratorController.Instance.JoinSession(_sessions[selectedDropdownValue].sessionId);
    }

    private void OnSessionJoined(Session session)
    {
        Debug.Log("Session joined: " + session.sessionName);
        Destroy(this.gameObject);
    }

    public void OnCreateSession()
    {
        OrchestratorController.Instance.OnAddSessionEvent = OnSessionCreated;
        OrchestratorController.Instance.AddSession("test-" + Guid.NewGuid().ToString());
    }

    private void OnSessionCreated(Session session)
    {
        Debug.Log("Session created: " + session.sessionName);
        Destroy(this.gameObject);
    }
}
