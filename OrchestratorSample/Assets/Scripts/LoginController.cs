using UnityEngine;
using Orchestrator.Wrapping;
using TMPro;
using UnityEngine.UI;

public class LoginController : MonoBehaviour
{
    public GameObject sessionSelector;
    public TMP_InputField usernameField;
    public TMP_InputField passwordField;
    public Button loginButton;

    private Orchestrator.App.Orchestrator _orchestrator;
    private bool _isConnected = false;

    private void Awake()
    {
        loginButton.interactable = false;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private async void Start()
    {
        var orchestratorUrl = ConfigLoader.Config.orchestratorUrl;
        Debug.Log("Connecting to orchestrator at: " + orchestratorUrl);
        _orchestrator = await OrchestratorController.Instance.SocketConnectAsync(orchestratorUrl);

        Debug.Log("Connected to orchestrator.");
        _isConnected = true;

        usernameField.onValueChanged.AddListener(delegate { loginButton.interactable = _isConnected && usernameField.text.Length > 0; });

        var version = await _orchestrator.GetOrchestratorVersion();
        Debug.Log("Version " + version);
    }

    public async void OnLoginClicked()
    {
        var username = usernameField.text;
        var password = passwordField.text;

        if (username.Length == 0)
        {
            return;
        }

        Debug.Log("Performing login using: " + username + " " + password);

        var userId = await _orchestrator.Login(username, (password != "") ? password : null);
        Debug.Log("Login successful. User ID: " + userId);

        Destroy(this.gameObject);
        Instantiate(sessionSelector);
    }
}
