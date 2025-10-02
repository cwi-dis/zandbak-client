using System.Collections.Generic;
using System.Linq;
using Orchestrator.Data;
using Orchestrator.Wrapping;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Session = Orchestrator.App.Session;

public class SessionSelectorController : MonoBehaviour
{
    [Header("Join Session")]
    public TMP_Dropdown sessionDropdown;
    public Button joinButton;

    [Header("Create Session")]
    public TMP_InputField sessionNameField;
    public Button createButton;

    [Header("Create Session")]
    public TMP_Dropdown scheduledSessionDropdown;
    public Button scheduleButton;

    private List<Session> _sessions;
    private List<ScheduledSession> _scheduledSessions;
    private Orchestrator.App.Orchestrator _orchestrator;

    private void Awake()
    {
        _orchestrator = OrchestratorController.Instance.Orchestrator;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private async void Start()
    {
        // Get active sessions
        var sessions = await _orchestrator.GetSessions();

        // Disable the join button if there are no active sessions
        if (sessions.Count == 0)
        {
            joinButton.interactable = false;
        }

        // Add session names to dropdown
        sessionDropdown.AddOptions(sessions.Select((s) => s.Name).ToList());
        joinButton.onClick.AddListener(OnJoinSession);

        // Refresh session dropdown when a session is created
        _orchestrator.OnSessionCreated += (_) =>
        {
            joinButton.interactable = true;

            sessionDropdown.ClearOptions();
            sessionDropdown.AddOptions(_orchestrator.Sessions.Select((s) => s.Name).ToList());

            _sessions = _orchestrator.Sessions;
        };

        // Add a listener to the session creation input field and only enable it if there is a value in the text input field
        createButton.interactable = false;
        sessionNameField.onValueChanged.AddListener(delegate { createButton.interactable = sessionNameField.text.Length > 0; });
        createButton.onClick.AddListener(OnCreateSession);

        _sessions = sessions;

        // Only show scheduled sessions dropdown if the user is a presenter
        if (_orchestrator.Self.Type == "presenter")
        {
            // Get scheduled sessions
            var scheduledSessions = await _orchestrator.GetScheduledSessions();

            if (scheduledSessions.Count == 0)
            {
                scheduleButton.interactable = false;
            }

            // Add scheduled sessions to the scheduled sessions dropdown
            scheduleButton.onClick.AddListener(OnScheduleSession);
            scheduledSessionDropdown.AddOptions(scheduledSessions.Select((s) => s.Title).ToList());

            _scheduledSessions = scheduledSessions;
        }
        else
        {
            scheduleButton.gameObject.SetActive(false);
            scheduledSessionDropdown.gameObject.SetActive(false);
        }
    }

    private async void OnJoinSession()
    {
        // Get the selected value and join the session using its ID
        var selectedDropdownValue = sessionDropdown.value;
        var joinedSession = await _orchestrator.JoinSession(_sessions[selectedDropdownValue].Id);

        OnSessionJoined(joinedSession);
    }

    private async void OnCreateSession()
    {
        // Get the chosen session name and create the session
        var createdSession = await _orchestrator.CreateSession(sessionNameField.text);
        OnSessionJoined(createdSession);
    }

    private async void OnScheduleSession()
    {
        // Get the selected value and create the scheduled session using its ID
        var selectedDropdownValue = sessionDropdown.value;
        var scheduledSession = await _orchestrator.ScheduleSession(_scheduledSessions[selectedDropdownValue].Id);

        OnSessionJoined(scheduledSession);
    }

    private void OnSessionJoined(Session session)
    {
        Debug.Log("Session joined: " + session.Name);

        // Destroy this object and load the session scene
        SceneManager.LoadScene("Scenes/SessionScene");
    }
}
