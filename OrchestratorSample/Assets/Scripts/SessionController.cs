using System.Collections.Generic;
using Orchestrator.Behaviour;
using Orchestrator.Data;
using Orchestrator.Wrapping;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using User = Orchestrator.App.User;
using Session = Orchestrator.App.Session;
using Bubble = Orchestrator.App.Bubble;

public class SessionController : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject localPlayerPrefab;
    public GameObject remotePlayerPrefab;

    [Header("Notifications")]
    public TMP_Text notificationField;

    [Header("Chat")]
    public TMP_InputField chatInputField;
    public Button chatSendButton;

    [Header("Raised Hands")]
    public TMP_Text raisedHandsField;
    public Button raiseHandButton;

    [Header("Presentation")]
    public TMP_Text presentationInfo;
    public Button nextPresentationButton;
    public Button nextSlideButton;
    public Button prevSlideButton;
    public Button resetPresentationButton;
    public Button sharePresentationButton;
    public MeshRenderer presentationCanvas;

    [Header("Session Management")]
    public Button leaveButton;
    public Button switchButton;
    public TMP_InputField sessionNameField;

    [Header("Bubbles")]
    public Button createBubbleButton;
    public Button inviteToBubbleButton;
    public Button leaveBubbleButton;
    public Button requestBubbleAccessButton;

    [Header("Panel Manager")]
    public PanelManager panelManager;

    private readonly Dictionary<string, GameObject> _activeUsers = new();
    private Session _session;
    private bool _isHandRaised = false;
    private bool _isSharingPresentation = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        // Getting the current session from Orchestrator singleton
        _session = OrchestratorController.Instance.Orchestrator.CurrentSession;

        // Attaching callback functions to session update events
        _session.OnUserJoined += OnUserJoined;
        _session.OnUserLeft += OnUserLeft;
        _session.OnMessageReceived += OnMessageReceived;
        _session.OnUserRaisedHand += OnUserRaisedHand;
        _session.OnUserClearedRaisedHand += OnUserClearedRaisedHand;
        _session.OnPresentationChanged += OnPresentationChanged;
        _session.OnPresentationSlideChanged += OnSlideChanged;
        _session.OnPresentationIsSharingChanged += OnPresentationShared;
        _session.OnClosed += OnSessionClosed;
        _session.OnUserStatusChanged += OnUserStatusChanged;
        _session.OnBubbleInvited += OnBubbleInvited;
        _session.OnBubbleJoinRequestApproved += OnBubbleJoinRequestApproved;

        // Adding listeners for UI elements
        leaveButton.onClick.AddListener(LeaveSession);
        switchButton.onClick.AddListener(SwitchSession);
        raiseHandButton.onClick.AddListener(RaiseOrLowerHand);
        chatSendButton.onClick.AddListener(SendChatMessage);
        chatInputField.onValueChanged.AddListener(delegate { chatSendButton.interactable = chatInputField.text.Length > 0; });

        createBubbleButton.onClick.AddListener(CreateBubble);
        inviteToBubbleButton.onClick.AddListener(InviteToBubble);
        leaveBubbleButton.onClick.AddListener(LeaveBubble);
        requestBubbleAccessButton.onClick.AddListener(RequestBubbleAccess);

        // Disable the chat send button initially
        chatSendButton.interactable = false;

        Debug.Log($"User type: {_session.Self.Type}");

        // Only enable presentation control buttons if there is at least one presentation
        if (_session.Presentations.Count > 0 && _session.Self.Type == "presenter")
        {
            nextPresentationButton.onClick.AddListener(NextPresentation);
            sharePresentationButton.onClick.AddListener(SharePresentation);

            // Go to the next slide
            nextSlideButton.onClick.AddListener(delegate { ChangeSlide(1); });
            // Go to the previous slide
            prevSlideButton.onClick.AddListener(delegate { ChangeSlide(-1); });
            // Reset presentation to the first slide
            resetPresentationButton.onClick.AddListener(delegate { SetSlide(0); });
        }
        else
        {
            nextPresentationButton.gameObject.SetActive(false);
        }

        // Disable slide control buttons until the first presentation is selected
        nextSlideButton.gameObject.SetActive(false);
        prevSlideButton.gameObject.SetActive(false);
        resetPresentationButton.gameObject.SetActive(false);
        sharePresentationButton.gameObject.SetActive(false);

        var user = _session.Self;
        Debug.Log($"Building session for user: {user.Name} ({user.Type}). Session has {_session.Users.Count} users already.");

        // Spawning avatars for users already in the session
        foreach (var remoteUser in _session.Users)
        {
            // Not spawning an avatar for self
            if (remoteUser.Id != user.Id)
            {
                Debug.Log($"Adding remote user {remoteUser.Name} ({remoteUser.Type}) with {remoteUser.Transform?.Bones?.Count} bones to session.");

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
        notificationField.text += $"Welcome to <i>{_session.Name}</i>\nThis room uses room model {_session.Room.Name}\n\n";
    }

    private async void NextPresentation()
    {
        await _session.GoToNextPresentation();
    }

    private async void SharePresentation()
    {
        await _session.SharePresentation(!_isSharingPresentation);
    }

    private async void ChangeSlide(int offset)
    {
        // Change current slide using offset
        await _session.ChangePresentationSlide(offset);
    }

    private async void SetSlide(int index)
    {
        // Set presentation slide to given index
        await _session.SetPresentationSlide(index);
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

    private async void LeaveSession()
    {
        await _session.Leave();
        await OrchestratorController.Instance.Orchestrator.Logout();

        SceneManager.LoadScene("Scenes/LoginScene");
    }

    private async void SwitchSession()
    {
        // Get the session name from the input field and trim it
        var sessionName = sessionNameField.text.Trim();

        // If the session name is empty, do nothing
        if (string.IsNullOrEmpty(sessionName))
        {
            return;
        }

        // Find the session by name
        var sessionToSwitchTo = await OrchestratorController.Instance.Orchestrator.FindSessionByName(sessionName);

        // If the session was not found, log a warning and return
        if (sessionToSwitchTo == null)
        {
            Debug.LogWarning($"Session with name {sessionName} not found.");
            return;
        }

        // Switch to the found session
        Debug.Log($"Switching to session {sessionToSwitchTo.Name} ({sessionToSwitchTo.Id})");
        await OrchestratorController.Instance.Orchestrator.SwitchSessions(sessionToSwitchTo.Id);

        // Reload current scene to update session information
        SceneManager.LoadScene("Scenes/SessionScene");
    }

    private async void RaiseOrLowerHand()
    {
        // Raising the current user's hand if it is not currently raised
        if (!_isHandRaised)
        {
            Debug.Log("Raising hand");
            await _session.Self.RaiseHand();

            // Changing text of button and setting flag
            _isHandRaised = true;
            raiseHandButton.GetComponentInChildren<TextMeshProUGUI>().text = "Lower hand";
        }
        else
        {
            // Lowering the current user's hand if it is already raised
            Debug.Log("Lowering hand");
            await _session.Self.ClearRaisedHand();

            // Changing text of button and clearing flag
            _isHandRaised = false;
            raiseHandButton.GetComponentInChildren<TextMeshProUGUI>().text = "Raise hand";
        }

        // Refresh the list of raised hands
        await _session.GetRaisedHands();
    }

    private async void CreateBubble()
    {
        // Create a new bubble
        var bubble = await _session.CreateBubble();

        bubble.OnJoinRequested += async (user) =>
        {
            Debug.Log($"User {user.Name} requests to join bubble");
            notificationField.text += $"<i>{user.Name} requests to join your bubble</i>\n";

            await bubble.ApproveBubbleJoinRequest(user, true);
        };

        MoveToBubble(bubble);
    }

    private void MoveToBubble(Bubble bubble)
    {
        // Set the position of the avatar to the position of the bubble plane
        var bubblePlane = GameObject.Find("BubblePlane");

        if (bubblePlane != null)
        {
            // Get plane position and size
            var planePosition = bubblePlane.transform.position;
            var planeSize = bubblePlane.GetComponent<Renderer>().bounds.size;

            // Pick random position within bubble plane
            planePosition.x += Random.Range(-planeSize.x / 2, planeSize.x / 2);
            planePosition.z += Random.Range(-planeSize.z / 2, planeSize.z / 2);

            // Set avatar position
            _session.Self.Avatar.transform.SetPositionAndRotation(planePosition, Quaternion.identity);
        }

        // Activate the BubblePanel
        panelManager.ActivatePanelByName("BubblePanel");

        bubble.OnUserJoined += (user) =>
        {
            Debug.Log($"User {user.Name} joined bubble");
            notificationField.text += $"<i>{user.Name} joined your bubble!</i>\n";
        };

        bubble.OnUserLeft += (user) =>
        {
            Debug.Log($"User {user.Name} left bubble");
            notificationField.text += $"<i>{user.Name} left your bubble!</i>\n";
        };
    }

    private async void InviteToBubble()
    {
        // Get other users in the session
        var otherUsers = _session.Users.FindAll(u => u.Id != _session.Self.Id);

        // If there are no other users in the session, do nothing
        if (otherUsers.Count == 0)
        {
            Debug.LogWarning("No other users in session, new bubble will only contain self.");
            return;
        }

        if (_session.CurrentBubble != null)
        {
            // Invite the first user in the session to the new bubble
            await _session.CurrentBubble.InviteUser(otherUsers[0]);
        }
    }

    private async void LeaveBubble()
    {
        if (_session.CurrentBubble != null)
        {
            // Invite the first user in the session to the new bubble
            await _session.CurrentBubble.LeaveBubble();

            panelManager.ActivatePanelByName("SessionPanel");
            _session.Self.Avatar.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }

    private async void RequestBubbleAccess()
    {
        // Get list of bubbles
        var bubbles = await _session.ListBubbles();

        // Do nothing is there are no bubbles
        if (bubbles.Count == 0)
        {
            Debug.LogWarning("No bubbles found.");
        }

        // Send join request to bubble owner
        await _session.RequestBubbleJoin(bubbles[0]);
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

    private async void OnSessionClosed()
    {
        Debug.Log("Session closed, loading login scene.");
        await OrchestratorController.Instance.Orchestrator.Logout();
        SceneManager.LoadScene("Scenes/LoginScene");
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

    private void OnUserLeft(User user, bool force) {
        Debug.Log("User " + user.Id + " left session");

        if (force)
        {
            Debug.Log("User " + user.Id + " was removed by an admin");

            // If the ID of the removed user is equal to Self and force is set to true, the current user was removed by
            // an admin. In that case, log out and load the login scene
            if (user.Id == _session.Self.Id)
            {
                Debug.Log("Self has been removed from session, logging out and loading login scene.");

                OrchestratorController.Instance.Orchestrator.Logout();
                SceneManager.LoadScene("Scenes/LoginScene");

                return;
            }
        }

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

    private async void OnPresentationChanged(Presentation presentation)
    {
        if (_isSharingPresentation)
        {
            await _session.SharePresentation(false);
        }

        // The variable 'presentation' is null if there are no more presentations
        if (presentation == null)
        {
            presentationInfo.text = "No more presentations";
            notificationField.text += $"<i>No more presentations</i>\n";

            // Disable all presentation control buttons
            nextPresentationButton.interactable = false;
            nextSlideButton.interactable = false;
            prevSlideButton.interactable = false;
            sharePresentationButton.interactable = false;

            return;
        }

        // Activate presentation control buttons if the current user is a presenter
        if (_session.Self.Type == "presenter")
        {
            nextSlideButton.gameObject.SetActive(true);
            prevSlideButton.gameObject.SetActive(true);
            resetPresentationButton.gameObject.SetActive(true);
            sharePresentationButton.gameObject.SetActive(true);
        }

        // Change status of current user to 'presenting'
        await _session.Self.SetStatus("presenting");

        // Find the user object for the presenter and update text fields
        var presenterUser = _session.FindUserById(presentation.Presenter);
        presentationInfo.text = $"<i>{presentation.Name}</i>\nby {presenterUser.Name}\n\n({presentation.NumSlides} slides)";
        notificationField.text += $"<i>Upcoming presentation: {presentation.Name}</i>\n";
    }

    private void OnSlideChanged(Presentation presentation)
    {
        // Find the user object for the presenter and update text fields
        var presenterUser = _session.FindUserById(presentation.Presenter);
        presentationInfo.text = $"<i>{presentation.Name}</i>\nby {presenterUser.Name}\n\n({presentation.NumSlides} slides)";
        notificationField.text += $"<i>Presentation slide changed to {presentation.CurrentSlide}</i>\n";
    }

    private void OnPresentationShared(Presentation presentation)
    {
        // Update isSharing flag
        _isSharingPresentation = presentation.IsSharing;

        // Find the user object for the presenter and update text fields
        var presenterUser = _session.FindUserById(presentation.Presenter);
        presentationInfo.text = $"<i>{presentation.Name}</i>\nby {presenterUser.Name}\n\n({presentation.NumSlides} slides)";
        notificationField.text += $"<i>Started presentation sharing</i>\n";

        presentationCanvas.gameObject.SetActive(_isSharingPresentation);
    }

    private void OnUserStatusChanged(User user)
    {
        // Post notification if a user in the session changes their status
        Debug.Log("User " + user.Name + " changed their status to: " + user.Status);
        notificationField.text += $"<i>{user.Name} changed their status to {user.Status}!</i>\n";
    }

    private void OnBubbleInvited(Bubble bubble)
    {
        Debug.Log("You have been invited to join the bubble: " + bubble.Name);
        notificationField.text += $"<i>You have been invited to join the bubble '{bubble.Name}' by {bubble.Owner.Name}</i>\n";

        _session.JoinBubble(bubble);
    }

    private void OnBubbleJoinRequestApproved(Bubble bubble, bool approved)
    {
        if (!approved)
        {
            Debug.Log($"Your join request for {bubble.Name} has been rejected");
            return;
        }

        Debug.Log($"You have been added to the bubble: {_session.CurrentBubble?.Name}");
        MoveToBubble(bubble);
    }
}
