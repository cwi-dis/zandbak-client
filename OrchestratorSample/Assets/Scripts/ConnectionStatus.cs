using UnityEngine;
using Orchestrator.Wrapping;

public class ConnectionStatus : MonoBehaviour
{
    private TMPro.TextMeshPro text;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        text = GetComponent<TMPro.TextMeshPro>();
        OrchestratorController.Instance.OnConnectionEvent += OnConnected;
    }

    private void OnConnected(bool connected)
    {
        if (connected)
        {
            text.text = "Connected";
        }
        else { 
            text.text = "Disconnected";
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
