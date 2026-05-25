using TMPro;
using UnityEngine;
using Orchestrator.Behaviour.Voice;

public class MicrophoneMeter : MonoBehaviour
{
    private TMP_Text _microphoneDebug;
    private VoiceTransmitter _transmitter;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _microphoneDebug = GetComponent<TMP_Text>();
        _transmitter = GetComponentInParent<VoiceTransmitter>();
        if (_transmitter) return;

        Debug.LogError("MicrophoneMeter requires a VoiceTransmitter component in its parent hierarchy.");
        _microphoneDebug.text = "Transmitter error";
    }

    // Update is called once per frame
    void Update()
    {
        if (!_transmitter) return;
        _microphoneDebug.text = _transmitter.IsTalking ? $"Talking ({_transmitter.Peak:F4})" : "Not talking";
    }
}
