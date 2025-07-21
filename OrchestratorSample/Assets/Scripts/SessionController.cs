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
    public TMP_InputField chatInputField;
    public Button chatSendButton;
    public TMP_Text raisedHandsField;
    public Button raiseHandButton;

    private readonly Dictionary<string, GameObject> _activeUsers = new();
    private Session _session;
    private bool _isHandRaised = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        // Getting the current session from Orchestrator singleton
        _session = OrchestratorController.Instance.Orchestrator.CurrentSession;

        // Attaching callback functions to events
        _session.OnUserJoined += OnUserJoined;
        _session.OnUserLeft += OnUserLeft;
        _session.OnMessageReceived += OnMessageReceived;
        _session.OnUserRaisedHand += OnUserRaisedHand;
        _session.OnUserClearedRaisedHand += OnUserClearedRaisedHand;

        // Adding listeners for UI elements
        raiseHandButton.onClick.AddListener(RaiseOrLowerHand);
        chatSendButton.onClick.AddListener(SendChatMessage);
        chatInputField.onValueChanged.AddListener(delegate { chatSendButton.interactable = chatInputField.text.Length > 0; });

        var user = _session.Self;
        Debug.Log($"Building session for user: {user.Name} ({user.Type}). Session has {_session.Users.Count} users already.");

        // Spawning avatars for users already in the session
        foreach (var remoteUser in _session.Users)
        {
            // Not spawning an avatar for self
            if (remoteUser.Id != user.Id)
            {
                Debug.Log($"Adding remote user {remoteUser.Name} ({remoteUser.Type}) with {remoteUser.Transform.Bones.Count} bones to session.");

                // Spawning remote avatar prefab and injecting user object dependency
                var remoteAvatar = Instantiate(remotePlayerPrefab).GetComponent<RemoteAvatar>();
                remoteAvatar.Initialize(remoteUser);
                // Adding instantiated user prefab to active user dictionary
                _activeUsers.Add(remoteUser.Id, remoteAvatar.gameObject);
            }
        }

        // Getting random spawn position for self
        var spawnPosition = new Vector3(
            Random.Range(-8, 8),
            0,
            Random.Range(-8, 8)
        );

        Debug.Log($"Spawning local player at {spawnPosition}");
        // Spawning local avatar prefab and injecting current user dependency
        var localAvatar = Instantiate(localPlayerPrefab, spawnPosition, Quaternion.identity).GetComponent<LocalAvatar>();
        localAvatar.Initialize(user);

        // Printing a welcome message
        notificationField.text += $"Welcome to <i>{_session.Name}</i>\n\n";
    }

    private async void SendChatMessage()
    {
        // Getting the message from the chat input field and clearing it
        var message = chatInputField.text;
        chatInputField.text = "";

        // Send a chat message if the message is not empty
        if (message.Length > 0)
        {
            await _session.SendMessage(message);
        }
    }

    private async void RaiseOrLowerHand()
    {
        // Raising the current user's hand if it is not currently raised
        if (!_isHandRaised)
        {
            Debug.Log("Raising hand");
            await _session.RaiseHand();

            // Changing text of button and setting flag
            _isHandRaised = true;
            raiseHandButton.GetComponentInChildren<TextMeshProUGUI>().text = "Lower hand";
        }
        else
        {
            // Lowering the current user's hand if it is already raised
            Debug.Log("Lowering hand");
            await _session.ClearRaisedHand();

            // Changing text of button and clearing flag
            _isHandRaised = false;
            raiseHandButton.GetComponentInChildren<TextMeshProUGUI>().text = "Raise hand";
        }

        // Refresh the list of raised hands
        await _session.GetRaisedHands();
    }

    private void OnUserClearedRaisedHand(User user)
    {
        // Some user raised their hand, add notification and refresh the list of raised hands
        notificationField.text += $"<i>{user.Name} lowered their hand!</i>\n";
        RenderRaisedHands();
    }

    private void OnUserRaisedHand(User user)
    {
        // Some user lowered their hand, add notification and refresh the list of raised hands
        notificationField.text += $"<i>{user.Name} raised their hand!</i>\n";
        RenderRaisedHands();
    }

    private async void RenderRaisedHands()
    {
        // Getting the list of raised hands and clearing raised hands field
        var raisedHands = await _session.GetRaisedHands();
        raisedHandsField.text = "";

        // Render the list of raised hands
        foreach (var raisedHandUser in raisedHands)
        {
            raisedHandsField.text += raisedHandUser.Name + "\n";
        }
    }

    private void OnMessageReceived(ChatMessage message)
    {
        // Append the received chat message to the notification field
        notificationField.text += $"{message.Sender.Username}: {message.Message}\n";
    }

    private void OnUserJoined(User user)
    {
        // A new user has joined, instantiate remote avatar prefab and inject the user object
        var remoteAvatar = Instantiate(remotePlayerPrefab).GetComponent<RemoteAvatar>();
        remoteAvatar.Initialize(user);

        Debug.Log("Spawning new user with id " + user.Id);
        // Add join notification and add the new user game object to the active user dictionary
        notificationField.text += $"<i>{user.Name} joined the session!</i>\n";
        _activeUsers.Add(user.Id, remoteAvatar.gameObject);
    }

    private void OnUserLeft(User user) {
        Debug.Log("User " + user.Id + "left session");

        // Check if the user is in active user dictionary, if so, remove and destroy the player object
        if (_activeUsers.TryGetValue(user.Id, out var obj))
        {
            Debug.Log("User found, removing and destroying player object");

            // Remove user from the active user dictionary, add notification and destroy the object
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
