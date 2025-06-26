using UnityEngine;
using Orchestrator.Wrapping;
using TMPro;

public class LoginController : MonoBehaviour
{
    public string orchestratorURL;
    public GameObject sessionSelector;
    public TMP_InputField usernameField;
    public TMP_InputField passwordField;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        OrchestratorController.Instance.SocketConnect(orchestratorURL);
    }

    public void OnLoginClicked()
    {
        var username = usernameField.text;
        var password = passwordField.text;

        Debug.Log("Performing login using: " + username + " " + password);

        OrchestratorController.Instance.OnLoginEvent += OnLoginResponse;

        if (password != "")
        {
            OrchestratorController.Instance.Login(username, password);
        }
        else
        {
            OrchestratorController.Instance.Login(username);
        }
    }

    private void OnLoginResponse(bool success, string userId)
    {
        Debug.Log("Login response: " + success + " " + userId);

        Destroy(this.gameObject);
        Instantiate(sessionSelector);
    }
}
