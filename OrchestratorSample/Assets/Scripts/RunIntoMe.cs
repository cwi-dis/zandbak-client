using Newtonsoft.Json.Linq;
using Orchestrator.Behaviour.Shared;
using Orchestrator.Data;
using UnityEngine;

public class RunIntoMe : MonoBehaviour
{
    private TriggerBehaviour _triggerBehaviour;
    private int _counter = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        _triggerBehaviour = GetComponent<TriggerBehaviour>();
        _triggerBehaviour.OnTriggerReceived += TriggerReceived;
    }

    private void TriggerReceived(TriggerData data)
    {
        _counter = data.Value.Value<int>("counter");
        Debug.Log($"Trigger received {_counter}");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Triggered");

        var data = new JObject { { "counter", _counter + 1 } };
        _triggerBehaviour.PublishTrigger(data);
    }
}
