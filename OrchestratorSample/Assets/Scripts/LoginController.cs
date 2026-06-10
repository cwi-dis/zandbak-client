using System.Linq;
using Orchestrator.Attributes;
using Orchestrator.ScriptableObjects;
using Orchestrator.Wrapping;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginController : MonoBehaviour
{
    [Header("Avatar Prefab")]
    [SerializeField]
    public AvatarPrefabRegistry prefabRegistry;

    [Header("Login Form Components")]
    [SerializeField]
    public TMP_InputField usernameField;
    [SerializeField]
    public TMP_InputField passwordField;
    [SerializeField]
    public TMP_Dropdown avatarDropdown;
    [SerializeField]
    public Button loginButton;

    [Header("Connection Status Field")]
    [SerializeField]
    public TMP_Text connectionStatusText;

    private Orchestrator.App.Orchestrator _orchestrator;
    private bool _isConnected = false;

    private void Awake()
    {
        // Disable all inputs on awake
        loginButton.interactable = false;
        usernameField.interactable = false;
        passwordField.interactable = false;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private async void Start()
    {
        // Get the Orchestrator URL from the config
        var orchestratorUrl = ConfigLoader.Config.OrchestratorUrl;
        Debug.Log("Connecting to orchestrator at: " + orchestratorUrl);

        // Attempt to connect to the Orchestrator if not connected already
        if (!OrchestratorController.Instance.ConnectedToOrchestrator)
        {
            _orchestrator = await OrchestratorController.Instance.SocketConnectAsync(orchestratorUrl);
        }
        else
        {
            _orchestrator = OrchestratorController.Instance.Orchestrator;
        }

        Debug.Log("Connected to orchestrator.");
        _isConnected = true;

        // Only enable the login button if at least the username field contains a value
        usernameField.onValueChanged.AddListener(delegate { loginButton.interactable = _isConnected && usernameField.text.Length > 0; });

        // Get the Orchestrator version and update the connection status text field
        var version = await _orchestrator.GetOrchestratorVersion();
        connectionStatusText.text = $"Connected to {orchestratorUrl}! Version: {version}";
        Debug.Log("Version " + version);

        // Enable text input fields
        usernameField.interactable = true;
        passwordField.interactable = true;

        // Populate the avatar dropdown with the available avatar names from the avatar prefab registry
        avatarDropdown.AddOptions(prefabRegistry.GetPrefabNames().ToList());
    }

    public async void OnLoginClicked()
    {
        var username = usernameField.text;
        var password = passwordField.text;

        // Do nothing if there was no username supplied
        if (username.Length == 0)
        {
            return;
        }

        var avatarPrefabName = avatarDropdown.options[avatarDropdown.value].text;
        Debug.Log($"Performing login using: {username} {password} {avatarPrefabName}");

        // Attempt to log in using the provided credentials. Pass null for the password if the password string is empty
        var user = await _orchestrator.Login(username, (password != "") ? password : null, OrchestratorController.DeviceType.Desktop, avatarPrefabName);
        Debug.Log("Login successful. User ID: " + user.Id);

        // Upon success, destroy this object and load the session selector scene
        SceneManager.LoadScene("Scenes/SessionSelectorScene");
    }
}
