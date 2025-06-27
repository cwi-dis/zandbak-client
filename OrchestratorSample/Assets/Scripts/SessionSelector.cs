using System;
using System.Collections.Generic;
using System.Linq;
using Orchestrator.App;
using Orchestrator.Wrapping;
using TMPro;
using UnityEngine;

public class SessionSelector : MonoBehaviour
{
    public TMP_Dropdown sessionDropdown;
    public GameObject sessionPrefab;

    private List<Session> _sessions;
    private Orchestrator.App.Orchestrator _orchestrator;

    private void Awake()
    {
        _orchestrator = OrchestratorController.Instance.Orchestrator;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private async void Start()
    {
        var sessions = await _orchestrator.GetSessions();

        sessionDropdown.AddOptions(sessions.Select((s) => s.Name).ToList());
        _sessions = sessions;
    }

    public async void OnJoinSession()
    {
        var selectedDropdownValue = sessionDropdown.value;
        var joinedSession = await _orchestrator.JoinSession(_sessions[selectedDropdownValue].Id);
        OnSessionJoined(joinedSession);
    }

    public async void OnCreateSession()
    {
        var createdSession = await _orchestrator.CreateSession("test-" + Guid.NewGuid());
        OnSessionJoined(createdSession);
    }

    private void OnSessionJoined(Session session)
    {
        Debug.Log("Session joined: " + session.Name);

        Destroy(this.gameObject);
        Instantiate(sessionPrefab);
    }
}
