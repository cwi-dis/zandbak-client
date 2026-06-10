using Newtonsoft.Json.Linq;
using Orchestrator.Behaviour.Shared;
using UnityEngine;

public class RunIntoMe : MonoBehaviour
{
    private TriggerBehaviour _triggerBehaviour;
    private int _counter = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        _triggerBehaviour = GetComponent<TriggerBehaviour>();
        _triggerBehaviour.onTriggerReceived.AddListener(TriggerReceived);
        _triggerBehaviour.onInitialized.AddListener(OnTriggerInitialized);
    }

    private void OnTriggerInitialized()
    {
        var triggerData = _triggerBehaviour.Data;

        if (triggerData != null && triggerData.IsPopulated)
        {
            _counter = triggerData.Value.Value<int>("counter");
            Debug.Log($"Initialised counter to {_counter}");
        }
    }

    private void TriggerReceived(float timestamp, JObject data)
    {
        _counter = data.Value<int>("counter");
        Debug.Log($"Trigger received {_counter} at time {timestamp}");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Triggered");

        var data = new JObject { { "counter", _counter + 1 } };
        _triggerBehaviour.PublishTrigger(data);
    }
}
