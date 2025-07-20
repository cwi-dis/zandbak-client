using UnityEngine;
using System.Collections.Generic;
using Orchestrator.Wrapping;
using Orchestrator.Behaviour;
using Orchestrator.Data;
using TMPro;
using User = Orchestrator.App.User;

public class SessionController : MonoBehaviour
{
    public GameObject localPlayerPrefab;
    public GameObject remotePlayerPrefab;
    public TMP_Text notificationField;

    private readonly Dictionary<string, GameObject> _activeUsers = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        var session = OrchestratorController.Instance.Orchestrator.CurrentSession;

        session.OnUserJoined += OnUserJoined;
        session.OnUserLeft += OnUserLeft;
        session.OnMessageReceived += OnMessageReceived;
        session.OnUserRaisedHand += OnUserRaisedHand;
        session.OnUserClearedRaisedHand += OnUserClearedRaisedHand;

        var user = session.Self;
        Debug.Log($"Building session for user: {user.Name} ({user.Type}). Session has {session.Users.Count} users already.");

        foreach (var remoteUser in session.Users)
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
    }

    void OnUserClearedRaisedHand(User user)
    {
        notificationField.text += user.Name + " lowered their hand!\n";
    }

    void OnUserRaisedHand(User user)
    {
        notificationField.text += user.Name + " raised their hand!\n";
    }

    void OnMessageReceived(ChatMessage message)
    {
        notificationField.text += message.Sender.Username + ": " + message.Message + "\n";
    }

    void OnUserJoined(User user)
    {
        var remoteAvatar = Instantiate(remotePlayerPrefab).GetComponent<RemoteAvatar>();
        remoteAvatar.Initialize(user);

        Debug.Log("Spawning new user with id " + user.Id);
        notificationField.text += user.Name + " joined the session!\n";
        _activeUsers.Add(user.Id, remoteAvatar.gameObject);
    }

    void OnUserLeft(User user) {
        Debug.Log("User " + user.Id + "left session");

        if (_activeUsers.TryGetValue(user.Id, out var obj))
        {
            Debug.Log("User found, removing and destroying player object");

            _activeUsers.Remove(user.Id);
            notificationField.text += user.Name + " left the session!\n";
            Destroy(obj);
        }
        else
        {
            Debug.LogWarning("Could not find object for user with id " + user.Id);
        }
    }
}
