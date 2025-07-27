using Orchestrator.Wrapping;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginController : MonoBehaviour
{
    public GameObject sessionSelector;
    public TMP_InputField usernameField;
    public TMP_InputField passwordField;
    public Button loginButton;
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
        // Attempt to connect to the Orchestrator
        _orchestrator = await OrchestratorController.Instance.SocketConnectAsync(orchestratorUrl);

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

        Debug.Log("Performing login using: " + username + " " + password);

        // Attempt to log sin using the provided credentials. Pass null for the password if the password string is empty
        var userId = await _orchestrator.Login(username, (password != "") ? password : null);
        Debug.Log("Login successful. User ID: " + userId);

        // Upon success, destroy this object and instantiate the session selector prefab
        SceneManager.LoadScene("Scenes/SessionSelector");
    }
}
