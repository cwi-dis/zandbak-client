using UnityEngine;
using System.Collections.Generic;
using Orchestrator.Wrapping;
using Orchestrator.Behaviour;
using Orchestrator.Data;
using TMPro;
using UnityEngine.UI;
using User = Orchestrator.App.User;
using Session = Orchestrator.App.Session;

public class SessionController : MonoBehaviour
{
    public GameObject localPlayerPrefab;
    public GameObject remotePlayerPrefab;

    public TMP_Text notificationField;
    public TMP_Text raisedHandsField;

    public Button raiseHandButton;

    private readonly Dictionary<string, GameObject> _activeUsers = new();
    private Session _session;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        _session = OrchestratorController.Instance.Orchestrator.CurrentSession;

        _session.OnUserJoined += OnUserJoined;
        _session.OnUserLeft += OnUserLeft;
        _session.OnMessageReceived += OnMessageReceived;
        _session.OnUserRaisedHand += OnUserRaisedHand;
        _session.OnUserClearedRaisedHand += OnUserClearedRaisedHand;

        raiseHandButton.onClick.AddListener(RaiseHand);

        var user = _session.Self;
        Debug.Log($"Building session for user: {user.Name} ({user.Type}). Session has {_session.Users.Count} users already.");

        foreach (var remoteUser in _session.Users)
        {
            if (remoteUser.Id != user.Id)
            {
                Debug.Log($"Adding remote user {remoteUser.Name} ({remoteUser.Type}) with {remoteUser.Transform.Bones.Count} bones to session.");

                var remoteAvatar = Instantiate(remotePlayerPrefab).GetComponent<RemoteAvatar>();
                remoteAvatar.Initialize(remoteUser);
                _activeUsers.Add(remoteUser.Id, remoteAvatar.gameObject);
            }
        }

        var spawnPosition = new Vector3(
            Random.Range(-8, 8),
            0,
            Random.Range(-8, 8)
        );

        Debug.Log($"Spawning local player at {spawnPosition}");
        var localAvatar = Instantiate(localPlayerPrefab, spawnPosition, Quaternion.identity).GetComponent<LocalAvatar>();
        localAvatar.Initialize(user);

        notificationField.text += $"Welcome to <i>{_session.Name}</i>\n\n";
    }

    private async void RaiseHand()
    {
        Debug.Log("Raising hand");

        await _session.RaiseHand();
        await _session.GetRaisedHands();
    }

    private void OnUserClearedRaisedHand(User user)
    {
        notificationField.text += $"<i>{user.Name} lowered their hand!</i>\n";
        RenderRaisedHands();
    }

    private void OnUserRaisedHand(User user)
    {
        notificationField.text += $"<i>{user.Name} raised their hand!</i>\n";
        RenderRaisedHands();
    }

    private async void RenderRaisedHands()
    {
        var raisedHands = await _session.GetRaisedHands();

        foreach (var raisedHandUser in raisedHands)
        {
            raisedHandsField.text += raisedHandUser.Username + "\n";
        }
    }

    private void OnMessageReceived(ChatMessage message)
    {
        notificationField.text += $"{message.Sender.Username}: {message.Message}\n";
    }

    private void OnUserJoined(User user)
    {
        var remoteAvatar = Instantiate(remotePlayerPrefab).GetComponent<RemoteAvatar>();
        remoteAvatar.Initialize(user);

        Debug.Log("Spawning new user with id " + user.Id);
        notificationField.text += $"<i>{user.Name} joined the session!</i>\n";
        _activeUsers.Add(user.Id, remoteAvatar.gameObject);
    }

    private void OnUserLeft(User user) {
        Debug.Log("User " + user.Id + "left session");

        if (_activeUsers.TryGetValue(user.Id, out var obj))
        {
            Debug.Log("User found, removing and destroying player object");

            _activeUsers.Remove(user.Id);
            notificationField.text += $"<i>{user.Name} left the session!</i>\n";
            Destroy(obj);
        }
        else
        {
            Debug.LogWarning("Could not find object for user with id " + user.Id);
        }
    }
}
