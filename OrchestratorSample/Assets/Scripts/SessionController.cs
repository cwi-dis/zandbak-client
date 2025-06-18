using UnityEngine;
using System.Collections.Generic;
using Orchestrator.Wrapping;
using Orchestrator.Data;
using Orchestrator.Behaviour;
using TMPro;
using UnityEngine.Rendering;

public class SessionController : MonoBehaviour
{
    public GameObject playerPrefab;
    public GameObject avatarPrefab;
    public TMP_Text notificationField;

    private Dictionary<string, GameObject> _activeUsers = new Dictionary<string, GameObject>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        OrchestratorController.Instance.OnUserJoinSessionEvent += OnUserJoined;
        OrchestratorController.Instance.OnUserLeaveSessionEvent += OnUserLeft;

        var user = OrchestratorController.Instance.SelfUser;
        Debug.Log("Building session for user: " + user.userName + " " + user.userType);

        if (user.userType == "presenter")
        {
            var self = Instantiate(avatarPrefab, new Vector3(8, 0, 8), Quaternion.identity);
            var avatar = self.GetComponentInChildren<AvatarNetworkBehaviour>();
            
            avatar.id = user.userId;
            avatar.isLocal = true;

            var controller = self.GetComponent<PlayerWalk>();
            controller.enabled = true;
        }
        else
        {
            var self = Instantiate(playerPrefab);
                
            var player = self.GetComponent<PlayerNetworkBehaviour>();
            player.id = user.userId;
            player.isLocal = true;

            var controller = self.GetComponent<PlayerController>();
            controller.enabled = true;
        }
    }

    void OnUserJoined(string userId, User user)
    {
        var spawnPosition = new Vector3(
            Random.Range(-8, 8),
            0,
            Random.Range(-8, 8)
        );
        var newPlayer = (user.userType == "presenter") ? Instantiate(avatarPrefab) : Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        var networkBehaviour = newPlayer.GetComponent<NetworkBehaviour>();
        networkBehaviour.id = userId;

        Debug.Log("Spawning new user with id " + userId);
        notificationField.text += user.userName + " joined the session!\n";
        _activeUsers.Add(userId, newPlayer);
    }

    void OnUserLeft(string userId) {
        Debug.Log("User " + userId + "left session");

        if (_activeUsers.TryGetValue(userId, out var obj))
        {
            Debug.Log("User found, removing and destroying player object");

            _activeUsers.Remove(userId);
            Destroy(obj);
        }
        else
        {
            Debug.LogWarning("Could not find object for user with id " + userId);
        }
    }
}
