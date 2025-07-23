using System.Collections.Generic;
using System.Linq;
using Orchestrator.App;
using Orchestrator.Wrapping;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SessionSelector : MonoBehaviour
{
    [Header("Join Session")]
    public TMP_Dropdown sessionDropdown;
    public Button joinButton;

    [Header("Create Session")]
    public TMP_InputField sessionNameField;
    public Button createButton;

    [Header("Create Session")]
    public TMP_InputField scheduledSessionIdField;
    public Button scheduleButton;

    [Header("Session Prefab")]
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

        if (sessions.Count == 0)
        {
            joinButton.interactable = false;
        }

        sessionDropdown.AddOptions(sessions.Select((s) => s.Name).ToList());
        joinButton.onClick.AddListener(OnJoinSession);

        createButton.interactable = false;
        sessionNameField.onValueChanged.AddListener(delegate { createButton.interactable = sessionNameField.text.Length > 0; });
        createButton.onClick.AddListener(OnCreateSession);

        scheduleButton.interactable = false;
        scheduledSessionIdField.onValueChanged.AddListener(delegate { scheduleButton.interactable = scheduledSessionIdField.text.Length > 0; });
        scheduleButton.onClick.AddListener(OnScheduleSession);

        _sessions = sessions;
    }

    private async void OnJoinSession()
    {
        var selectedDropdownValue = sessionDropdown.value;
        var joinedSession = await _orchestrator.JoinSession(_sessions[selectedDropdownValue].Id);
        OnSessionJoined(joinedSession);
    }

    private async void OnCreateSession()
    {
        var createdSession = await _orchestrator.CreateSession(sessionNameField.text);
        OnSessionJoined(createdSession);
    }

    private async void OnScheduleSession()
    {
        var scheduledSession = await _orchestrator.ScheduleSession(scheduledSessionIdField.text);
        OnSessionJoined(scheduledSession);
    }

    private void OnSessionJoined(Session session)
    {
        Debug.Log("Session joined: " + session.Name);

        Destroy(this.gameObject);
        Instantiate(sessionPrefab);
    }
}
