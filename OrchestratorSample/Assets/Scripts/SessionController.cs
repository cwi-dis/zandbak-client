using UnityEngine;
using System.Collections.Generic;
using Orchestrator.Wrapping;
using Orchestrator.Behaviour;
using Orchestrator.Data;
using TMPro;
using User = Orchestrator.App.User;

public class SessionController : MonoBehaviour
{
    public GameObject playerPrefab;
    public GameObject avatarPrefab;
    public TMP_Text notificationField;

    private Dictionary<string, GameObject> _activeUsers = new();

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
        Debug.Log("Building session for user: " + user.Name + " " + user.Type);

        if (user.Type == "presenter")
        {
            var self = Instantiate(avatarPrefab, new Vector3(8, 0, 8), Quaternion.identity);
            var avatar = self.GetComponentInChildren<AvatarNetworkBehaviour>();

            avatar.id = user.Id;
            avatar.isLocal = true;

            var controller = self.GetComponent<PlayerWalk>();
            controller.enabled = true;
        }
        else
        {
            var self = Instantiate(playerPrefab);

            var player = self.GetComponent<PlayerNetworkBehaviour>();
            player.id = user.Id;
            player.isLocal = true;

            var controller = self.GetComponent<PlayerController>();
            controller.enabled = true;
        }
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
        var spawnPosition = new Vector3(
            Random.Range(-8, 8),
            0,
            Random.Range(-8, 8)
        );
        var newPlayer = (user.Type == "presenter") ? Instantiate(avatarPrefab) : Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        var networkBehaviour = newPlayer.GetComponent<NetworkBehaviour>();
        networkBehaviour.id = user.Id;

        Debug.Log("Spawning new user with id " + user.Id);
        notificationField.text += user.Name + " joined the session!\n";
        _activeUsers.Add(user.Id, newPlayer);
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
